
using Microsoft.EntityFrameworkCore;
using WebHealth.Server.Data;
using WebHealth.Server.Hubs;
using WebHealth.Server.Models;
using WebHealth.Server.Services;

namespace WebHealth.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite("Data Source=webhealth.db"));

            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IHealthMetricsCache, HealthMetricsCache>();
            builder.Services.AddTransient<MockHostCheckService>();
            builder.Services.AddTransient<RealHostCheckService>();
            builder.Services.AddTransient<IHostCheckerEngine, HostCheckerEngine>();
            builder.Services.AddSignalR();
            builder.Services.AddHostedService<HealthCheckBackgroundWorker>();

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.SetIsOriginAllowed(_ => true)//any port for vite
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                db.Database.ExecuteSqlRaw("UPDATE Targets SET IsActive = 1;");
            }

            app.UseDefaultFiles();
            app.MapStaticAssets();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseCors();

            app.MapControllers();

            app.MapFallbackToFile("/index.html");


            app.MapGet("/api/test-api", async (AppDbContext db, IHostCheckerEngine engine, IHealthMetricsCache cache) => 
            {
                var targets = await db.Targets.ToListAsync();
                IList<object> phaseResults = new List<object>();
                foreach (var target in targets)
                {
                    var snapshot = await engine.ExecuteCheckCycleAsync(target);
                    phaseResults.Add(new
                    {
                        Name = target.Name,
                        Status = snapshot.Status.ToString(),
                        LatencyMs = cache.CalculateAverageLatency(target.Id),
                        Uptime = cache.CalculateUptime(target.Id),
                        Phases = snapshot.PhaseResults,
                    });
                }
                return Results.Ok(phaseResults);
            });

            app.MapHub<HealthHub>("/healthHub");
            app.MapGet("/api/hosts", async (AppDbContext db) => await db.Targets.ToListAsync());
            app.MapPost("/api/hosts", async (AppDbContext db, HealthTarget target) =>
            {
                target.Id = Guid.NewGuid();
                db.Targets.Add(target);
                await db.SaveChangesAsync();
                return Results.Created($"/api/hosts/{target.Id}", target);
            });
            app.MapDelete("/api/hosts/{id:guid}", async (AppDbContext db, Guid id) =>
            {
                var target = await db.Targets.FindAsync(id);
                if (target == null) return Results.NotFound();
                db.Targets.Remove(target);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            app.Run();
        }
    }
}

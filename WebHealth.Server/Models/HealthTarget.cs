namespace WebHealth.Server.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

public class HealthTarget
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
	public bool IsMock { get; set; } = false;
	public bool IsActive { get; set; } = true;
	public int IntervalSeconds { get; set; } = 5;
	[NotMapped]
	public HealthStatus Status { get; set; } = HealthStatus.Unknown;
}

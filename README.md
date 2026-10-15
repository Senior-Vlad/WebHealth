# WebHealth — Real-Time Distributed Health & Latency Monitor

**WebHealth** is a real-time web application designed to monitor the availability (uptime), HTTP status codes, and response latency of web services. Built with a multi-threaded **.NET 8** backend and a reactive **React + TypeScript** dashboard, it streams live health metrics directly to connected clients using **SignalR (WebSockets)**.

---

## Why This Project? (The Problem It Solves)

When monitoring dozens of external endpoints, naive sequential polling in a simple loop is too slow, while uncontrolled parallel execution leads to **Socket Exhaustion**, firewall rate-limiting, and **Race Conditions** when aggregating metrics.

**WebHealth addresses these engineering challenges through:**
1. **Controlled Concurrency** — Limits the maximum number of simultaneous outbound HTTP requests to prevent network and socket pool overload.
2. **Barrier-Synchronized Cycles** — Coordinates worker threads so that all checks within a cycle complete before aggregating a unified snapshot for the frontend.
3. **Real-Time Push Architecture** — Instead of relying on wasteful client-side HTTP polling, the backend actively broadcasts updated telemetry via WebSockets immediately after each verification phase.
4. **Dual-Mode Execution (Real & Mock)** — Supports both live HTTP network probing and a safe **Mock Mode** to simulate high latency, timeouts, and server outages (`500 Internal Server Error`, `503 Service Unavailable`).

---

## Tech Stack

### Backend (.NET 8 / C#)
* **ASP.NET Core Minimal API** — Lightweight REST endpoints for managing monitored hosts, toggling execution modes, and controlling check cycles.
* **SignalR Core** — Bi-directional real-time communication for instant metric broadcasting to connected clients.
* **BackgroundService (`IHostedService`)** — Long-running background worker that orchestrates periodic health check rounds independently of user HTTP requests.
* **Entity Framework Core + SQLite** — Persistent relational storage for target host configurations and application state.

### Concurrency & Synchronization (Under the Hood)
* **`SemaphoreSlim`** — Enforces request throttling by limiting the number of concurrent outbound HTTP calls.
* **`Barrier`** — Synchronizes multiple worker threads across phases, ensuring every worker finishes its assigned host checks before triggering the post-phase SignalR broadcast.
* **`ConcurrentDictionary`** — Provides lock-free, thread-safe in-memory storage and atomic updates for the live state of each monitored endpoint.

### Frontend (React + TypeScript)
* **React + Vite + TypeScript** — Fast, strictly-typed single-page application (SPA) for real-time telemetry visualization.
* **`@microsoft/signalr`** — Client WebSocket SDK encapsulated in a custom `useHealthCheckHub` hook with automatic reconnection handling.
* **Tailwind CSS** — Responsive UI styling with dynamic status indicators (`Healthy`, `Degraded`, `Down`) and latency badges.

---

## Project Structure

```text
WebHealth/
├── WebHealth.Backend/                  # .NET 8 Server Application
│   ├── Data/                           # EF Core DbContext, entities, and SQLite migrations
│   ├── Engine/                         # Multi-threaded verification core (HostCheckerEngine)
│   │   └── Synchronization primitives: Barrier, SemaphoreSlim, ConcurrentDictionary
│   ├── Services/                       # Background worker (HealthCheckBackgroundWorker)
│   ├── Hubs/                           # SignalR Hub (HealthCheckHub) for live metric streaming
│   ├── Models/                         # Domain entities and health check DTOs
│   ├── Endpoints/                      # Minimal API route definitions for host management
│   └── Program.cs                      # DI container setup, CORS, SignalR, and middleware pipeline
│
└── WebHealth.Frontend/                 # React + TypeScript + Vite Client Application
    ├── src/
    │   ├── components/                 # Dashboard UI components, status cards, and forms
    │   ├── hooks/                      # Custom useHealthCheckHub hook for SignalR lifecycle
    │   ├── types/                      # TypeScript interfaces for telemetry and host states
    │   └── App.tsx                     # Main real-time monitoring view
    ├── package.json
    └── tailwind.config.js
```

---

## How It Works (Check Lifecycle)

1. **Initialization:** On startup, `Program.cs` initializes the SQLite database, loads the configured target URLs into a thread-safe `ConcurrentDictionary`, and starts the `HealthCheckBackgroundWorker`.
2. **Throttling (`SemaphoreSlim`):** Each endpoint is evaluated asynchronously. Before firing an HTTP request, the worker acquires a slot from the semaphore to respect concurrency limits.
3. **Probing (Real or Mock):** `HostCheckerEngine` measures exact round-trip latency using `Stopwatch` (or generates synthetic failures in Mock Mode) and atomically updates the host's record in the `ConcurrentDictionary`.
4. **Synchronization (`Barrier`):** Once a worker completes its check, it calls `SignalAndWait()` on the barrier. When all participating threads reach the barrier, the post-phase action is triggered.
5. **Broadcasting (`SignalR`):** The aggregated snapshot of all hosts is pushed via `IHubContext` to all connected clients, immediately updating the React UI without page reloads.

---

## Getting Started (Local Setup)

### Prerequisites
* **.NET 8 SDK**
* **Node.js** (v18+ or v20+ recommended)

### 1. Run the Backend Server
```bash
# Navigate to the backend directory
cd WebHealth.Backend

# Restore dependencies and start the server
dotnet restore
dotnet run
```
*The API and SignalR Hub will start on your local port (e.g., `http://localhost:5000` or `https://localhost:7001`), and the SQLite database file will be created automatically.*

### 2. Run the Frontend Client
Open a new terminal window:
```bash
# Navigate to the frontend directory
cd WebHealth.Frontend

# Install dependencies and start the Vite dev server
npm install
npm run dev
```
*Open your browser at the URL shown in the terminal (typically `http://localhost:5173`).*

---

## Key Features

* **Live Telemetry Dashboard:** Visual status indicators (`Online`, `High Latency`, `Offline`), HTTP response codes, and real-time latency measurements in milliseconds (`ms`).
* **Dynamic Host Management:** Add new target URLs or remove existing ones on the fly without restarting the server.
* **Failure Simulation (Mock Mode):** Test system resilience and UI responsiveness under simulated high-load, timeout, and server-crash scenarios.

# Project Structure

## Solution Layout

```
ResilienceSignalRDemo.sln
├── ConfigApi/                        # Central configuration & SignalR hub
├── ProducerApi/                      # Simulated unreliable producer
├── ConsumerApi/                      # Resilient consumer (Microsoft.Extensions.Http.Resilience)
├── ManagementDashboard/              # Blazor Server UI for monitoring/management
├── AspireStarterApp.ServiceDefaults/ # Shared OpenTelemetry, health checks, service defaults
└── PolyDemoAppHost/                  # Aspire AppHost orchestrating all services
```

## Dependency Graph

```
AspireStarterApp.ServiceDefaults   [no project deps, shared code]
       ↑
ConsumerApi     ← references ServiceDefaults
ProducerApi     ← references ServiceDefaults
ConfigApi       ← references ServiceDefaults
ManagementDashboard  ← references ServiceDefaults
       ↑
PolyDemoAppHost ← orchestrates ConsumerApi, ProducerApi, ConfigApi, ManagementDashboard
```

## Target Framework

All projects target `net10.0`.

## Key NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.Extensions.Http.Resilience` | Retry + circuit-breaker pipeline |
| `Microsoft.AspNetCore.SignalR.Client` | Real-time config updates between services |
| `prometheus-net.AspNetCore` | Prometheus metrics exposition |
| `Aspire.Hosting.AppHost` | AppHost orchestration |
| `OpenTelemetry.*` | Distributed tracing and metrics |

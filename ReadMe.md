# ResilienceSignalRDemo

> **Naming note:** This repository was originally named `PollyDemo` during early development. The codebase migrated from standalone Polly to `Microsoft.Extensions.Http.Resilience`, and some project names (e.g. `PolyDemoAppHost`) still reflect the old naming.

## Objectives

- Demonstrate retry and circuit-breaker patterns in .NET 10 using `Microsoft.Extensions.Http.Resilience`.
- Provide a minimal Producer API that randomly fails.
- Provide a Consumer API that applies resilience policies via `HttpClientFactory`.
- Offer a Blazor Server management dashboard to view metrics and adjust resilience settings at runtime.

---

## Projects (what they are and why they exist)

1. `ConfigApi`  
   - Purpose: central configuration & coordination service.  
   - Features: SignalR hub at `/confighub` for broadcasting configuration (for example: Producer error chance), simple health and config endpoints used by other services.

2. `ProducerApi`  
   - Purpose: simulated unreliable producer service.  
   - Features: minimal API with `GET /produce` that fails based on a configurable error chance; connects to `ConfigApi` via SignalR (so the Producer can receive live updates); exposes Prometheus metrics at `/prometheus`.

3. `ConsumerApi`  
   - Purpose: demonstrates resilient consumption of `ProducerApi`.  
   - Features: minimal API with `GET /consume` that calls `ProducerApi` using an `HttpClient` pipeline configured with `Microsoft.Extensions.Http.Resilience` retry + circuit-breaker strategies; exposes runtime JSON metrics via `GET /metrics` and current resilience configuration via `GET /config` / `PUT /config`; Prometheus metrics available at `/prometheus`.

4. `ManagementDashboard` (Blazor Server)  
   - Purpose: real-time UI for monitoring and managing the demo.  
   - Features: interactive Blazor Server pages that display total calls, retries fired, circuit state, last reset time; lets you update resilience settings (retry count, backoff, breaker thresholds); talks to `ConfigApi` SignalR hub to broadcast updates and calls `ConsumerApi` endpoints to read metrics and apply config.

5. `AspireStarterApp.ServiceDefaults`  
   - Purpose: shared service defaults used by each service (health checks, OpenTelemetry, service discovery, etc.).  
   - Features: helper extensions such as `AddServiceDefaults()` to wire OpenTelemetry, health checks and reasonable HTTP client defaults used across projects.

6. `PolyDemoAppHost` (AppHost / Aspire)  
   - Purpose: centralized development host that orchestrates the projects for local development.  
   - Features: brings up `ConfigApi`, `ProducerApi`, `ConsumerApi`, and `ManagementDashboard` together so you can run the whole demo from a single host process.

---

## Prerequisites

- .NET 10 SDK installed
- .NET Aspire workload (required for the AppHost project; install via `dotnet workload install aspire`)
- Docker (for Prometheus + Grafana)
- (Optional) Visual Studio 2022+ or VS Code

---

## How to start the solution (recommended - centralized)

1. From the command line (root of the repo) run the Aspire AppHost which will start all local projects:

```bash
# start the Aspire app host which orchestrates all services
dotnet run --project PolyDemoAppHost
```

2. Or in Visual Studio: set the startup project to `PolyDemoAppHost` and run (F5 or Debug -> Start Debugging). This starts `ConfigApi`, `ProducerApi`, `ConsumerApi` and the Blazor `ManagementDashboard` together.

Notes:
- `PolyDemoAppHost` is the single entry point for the demo; you should not need to start each project manually when using the AppHost.

---

## Observability (Prometheus & Grafana)

Prometheus and Grafana are used for metrics and dashboards. They are started separately via `docker-compose` so you can control whether you want the external stack running.

From the repository root run:

```bash
# start Prometheus and Grafana (runs the containers defined in docker-compose.yml)
docker-compose up -d
```

- Grafana UI: `http://localhost:3000` (default admin password is `admin` as configured in `docker-compose.yml`)  
- Prometheus UI: `http://localhost:9090`

Prometheus scrapes the metrics endpoints exposed by the services. ProducerApi and ConsumerApi expose Prometheus endpoints at `/prometheus`. The `ConsumerApi` also publishes a JSON `/metrics` endpoint consumed by the dashboard for quick operational data.

---

## Quick testing and examples

- Call `ConsumerApi` directly (this exercises the resilience policies):

```bash
curl http://localhost:5001/consume
```

- Open the management dashboard (Blazor Server) in a browser:

```
http://localhost:5002
```

Dashboard actions you can perform:
- Refresh metrics to see: total calls, retries fired, circuit state and last reset.
- Update resilience configuration (retry count, backoff milliseconds list, exceptions before breaking, break duration) and click Update to apply it to `ConsumerApi`.
- Start the Producer's hub connection from the dashboard (button provided) so the Producer can receive live config updates.
- Run multiple successive test calls (parallel or sequential) to observe retries and circuit breaker behavior.

---

## Endpoints of interest

- `ConfigApi`:
  - SignalR hub: `/confighub`  
  - Health: `/health`
- `ProducerApi`:
  - Produce: `GET /produce` (simulated failures)  
  - Prometheus metrics: `/prometheus`
- `ConsumerApi`:
  - Consume: `GET /consume`  
  - Metrics (JSON for dashboard): `GET /metrics`  
  - Resilience config: `GET /config`, `PUT /config`  
  - Prometheus metrics: `/prometheus`
- `ManagementDashboard`:
  - Blazor UI served by the AppHost (default `http://localhost:5002` when run from AppHost)

---

## What You’ve Learned
- How to wire up retry and circuit-breaker policies via `Microsoft.Extensions.Http.Resilience` in an `HttpClientFactory` pipeline.
- Capturing retry/fail metrics in a singleton store.
- Dynamically updating resilience policies at runtime via a dashboard.
- Blazor Server as a quick UI for operational insights.

---

## Notes and next steps

- Use the dashboard to experiment with retry counts, backoff and circuit breaker thresholds to see how the resilience pipeline reacts to transient failures.
- To collect traces/metrics centrally you can configure OpenTelemetry exporters; `AspireStarterApp.ServiceDefaults` contains helpers to enable OTLP exporters using the `OTEL_EXPORTER_OTLP_ENDPOINT` configuration.
- Add unit and integration tests to simulate failures and assert expected retry/circuit behaviors.
- Optionally dockerize the whole stack and use `docker-compose` to run every component (services + observability).

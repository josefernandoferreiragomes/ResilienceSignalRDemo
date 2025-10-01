# PollyDemo

## Objectives

- Demonstrate retry and circuit-breaker patterns in .NET 8 using Polly.
- Provide a minimal Producer API that randomly fails.
- Provide a Consumer API that applies resilience policies via `HttpClientFactory`.
- Offer a Blazor Server dashboard to view metrics and adjust resilience settings at runtime.

---

## Projects

1. **ProducerApi**  
   - Minimal API  
   - GET `/produce` fails 30% of the time  

2. **ConsumerApi**  
   - Minimal API  
   - GET `/consume` calls ProducerApi  
   - Polly Retry + Circuit Breaker via DI  
   - Exposes `/metrics` and `/config`  

3. **Dashboard**  
   - Blazor Server  
   - Displays runtime metrics  
   - Allows updating retry counts, back-off, breaker thresholds  

---

## Prerequisites

- .NET 8 SDK installed  
- (Optional) Visual Studio 2022+ or VS Code  

---

## Running via CLI

1. Build solution  
```bash
cd PollyDemo
dotnet build
```

2. Launch ProducerApi (port 5000)
```bash
dotnet run --project ProducerApi --urls http://localhost:5000
```

3. Launch ConsumerApi (port 5001)
```bash
dotnet run --project ConsumerApi --urls http://localhost:5001
```

4. Launch Dashboard (port 5002)
```bash
dotnet run --project Dashboard --urls http://localhost:5002
```

5. Browse to http://localhost:5002

## Testing & Adjusting

- Hit ConsumerApi directly:

```bash
curl http://localhost:5001/consume
```

- Open Dashboard, click Refresh to see:
-- Total calls
-- Retries fired
-- Circuit state + last reset
- Modify configuration fields (e.g., change retry count to 5), click Update, then Refresh.

## What You’ve Learned
- How to wire up Polly Retry and Circuit Breaker in an HttpClientFactory pipeline.
- Capturing retry/fail metrics in a singleton store.
- Dynamically updating resilience policies at runtime via a dashboard.
- Blazor Server as a quick UI for operational insights.

## 5. Next Steps & Tips

- Add unit tests to simulate transient failures.  
- Dockerize each service and orchestrate with `docker-compose`.  
- Extend with additional policies: timeout, bulkhead isolation.
- Add prometheus
https://devblogs.microsoft.com/dotnet/introducing-aspnetcore-metrics-and-grafana-dashboards-in-dotnet-8/
- Secure Dashboard and APIs with JWT or OAuth2. 

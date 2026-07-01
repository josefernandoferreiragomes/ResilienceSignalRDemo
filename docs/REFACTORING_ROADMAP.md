# Refactoring Roadmap

## Completed Phases

- **Phase 1**: Initial project setup with .NET + Polly
- **Phase 2**: SignalR integration for live config broadcasting
- **Phase 3**: Aspire AppHost orchestration
- **Phase 4**: Prometheus/Grafana observability
- **Phase 5**: Blazor ManagementDashboard
- **Phase 6**: Migration from standalone Polly to `Microsoft.Extensions.Http.Resilience`
- **Phase 7**: README cleanup, Dashboard removal, governance docs creation

## Remaining Work

- [ ] Fix naming inconsistency: `PolyDemoAppHost` (typo) → `PollyDemoAppHost` or align with repo name
- [ ] Add unit/integration tests for resilience pipeline behavior
- [ ] Harmonize NuGet package versions across projects (Aspire.Hosting 9.5.0 vs 13.4.4, etc.)
- [ ] Pin .NET SDK version via `global.json`
- [ ] Evaluate whether `ManagementDashboard/` hardcoded ports should use Aspire service discovery

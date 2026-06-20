# Copilot Instructions (workspace .copilot)

## Solution goal
This solution demonstrates resilient calls with Polly and shows runtime metrics and timelines in a Blazor dashboard. The immediate objective is to provide clear timeline visualizations (circuit breaker state, producer responses, consumer attempts/retries) with meaningful time granularity, accurate status/error representation, and interactive tooltips so the dashboard can be used to teach circuit-breaker and retry behavior.

## Architecture summary
- Multi-project .NET 10 solution with a Blazor (server or interactive server) management dashboard (ManagementDashboard).
- Dashboard fetches timeline events from the ConsumerApi endpoint `timeline?minutes=30`.
- Charts are rendered client-side using Chart.js via JavaScript interop (ManagementDashboard/wwwroot/timelineCharts.js) and canvases on ManagementDashboard/Components/Pages/Management.razor.
- SignalR is used for ConfigHub updates (hub connection in Management.razor).
- The dashboard consumes metrics and config from ConsumerApi and ProducerApi via named HttpClient instances.

Notes / Uncertainty:
- The server-side timeline event model and producer/consumer event emission code were not found in the dashboard project. It is likely present in the ConsumerApi or ProducerApi projects. Changes that require new fields (e.g., CorrelationId, always providing StatusCode, ISO timestamps) will need updates server-side; those files were not inspected here.

## Project descriptions
- ManagementDashboard: Blazor interactive UI for metrics and timeline charts. Contains Management.razor and timelineCharts.js.
- ConsumerApi (observed by configuration): provides metrics and timeline endpoints consumed by the dashboard.
- ProducerApi (observed by configuration): produces events and can be started from the dashboard.
- ConfigApi: SignalR hub for configuration updates (error chance).

(If additional projects exist in the solution they were not enumerated in the dashboard files inspected; see the solution in Visual Studio for full list.)

## Key patterns and dependencies
- Blazor InteractiveServer rendering pattern in Management.razor.
- JavaScript interop for charts: IJSRuntime.InvokeVoidAsync to call functions in timelineCharts.js.
- Chart.js used in timelineCharts.js.
- HttpClientFactory for named clients ("ConsumerApi", "ProducerApi", "ConfigApi").
- Strategy: minimal cross-service changes preferred; prefer using ConsumerApi timeline endpoint for chart data (per existing project guideline).

## Observed issues (for context)
- Timestamp labels use second precision (HH:mm:ss) causing multiple events within the same second to overlap and appear coarse.
- Producer simulated errors may be emitted as exception events without StatusCode; current filter `StatusCode.HasValue` omits those, hiding error points.
- Retry detection in Management.razor relies on Detail containing the word "retry", which is fragile. Also, label truncation hides multiple attempts occurring within the same second.
- Chart x axis uses category labels (string labels). Using a time scale (Chart.js 'time' axis) with ISO timestamps would yield accurate horizontal spacing.

## Coding conventions
- Target framework: .NET 10.
- Blazor components: follow existing project conventions (InteractiveServer on Management.razor).
- Maintain minimal cross-service impact: prefer client-side formatting and mapping where possible, but prefer server instrumentation for robust correlation (CorrelationId / RequestId) and status codes.
- JavaScript files should gracefully handle Chart.js unavailability (as timelineCharts.js currently does).
- Keep debug logging minimal and easily removable (Console.WriteLine in Blazor components acceptable for debugging).

## Workflow rules
- Always inspect the code first (search and open files) before drafting a plan.
- When changes affect multiple services (ConsumerApi / ProducerApi), identify required server-side changes and propose them; do not change them without confirmation.
- Use small, incremental changes and test locally in Visual Studio (preferred shell: PowerShell).
- Use named HttpClient instances as configured; do not hardcode endpoints.

## How to request changes
- Open an issue or create a pull request on the repository. Provide:
  - short problem description,
  - expected behavior,
  - screenshot or reproduction steps,
  - proposed scope (UI only / server changes required).
- For UI timeline issues, include a sample timeline payload (JSON) from the ConsumerApi timeline endpoint to allow reproducing timestamp, kind, and detail shapes.

## How to generate implementation plans
- Implementation plan should include:
  - problem summary,
  - proposed code and non-code changes,
  - affected files,
  - step-by-step tasks (ordered),
  - validation steps and tests,
  - estimated risks and mitigation.
- Use .copilot/plans/implementation_plan_<yyyymmddhhmm>.md naming for plans. Each plan is a snapshot and must be registered in the plan file.

## How to update the changelog
- Append-only: create or append to `.copilot/changelog.md`.
- Entries must include:
  - timestamp (UTC, `yyyymmdd-hhmm`),
  - what changed,
  - why,
  - affected files.
- Do not rewrite previous entries.

## Local debugging tips for timeline charts
- Inspect the raw `/timeline` payload to confirm:
  - timestamps include milliseconds and are in ISO format,
  - events include Kind, TimestampUtc, StatusCode, Detail, CorrelationId (if present).
- If millisecond precision is missing server-side, prefer emitting ISO timestamps (DateTimeOffset.UtcNow.ToString("o")).
- To avoid label collisions in UI, either:
  - use string labels with milliseconds ("HH:mm:ss.fff"), or
  - switch the Chart.js x-axis to a 'time' axis and pass ISO timestamps as x values.
- For exceptions produced by Producer: prefer emitting a StatusCode (500) or add an ErrorCode field; dashboard can interpret exceptions as 500 if server-side change is not possible immediately.

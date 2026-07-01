# Implementation Plan — Timeline & Chart Improvements (20260620-0900)

Problem summary
- Timeline charts in ManagementDashboard show coarse time granularity (second-level), causing events that occur within the same second to collapse visually.
- Producer simulated errors are not consistently represented: events emitted as exceptions without StatusCode are omitted by dashboard filters or shown as 0.
- Retry marking for consumer attempts is fragile (only checks Detail for "retry") and can miss retries.
- Charts use category x-axis with string labels leading to incorrect horizontal spacing; a time-based axis would provide correct spacing.

Proposed changes
- Dashboard (Management.razor)
  - Use millisecond precision for labels when rendering current string-label approach ("HH:mm:ss.fff").
  - Enhance producer event selection to include events with Detail/exception and map null StatusCode -> 500 (or a sentinel) so they appear as error points.
  - Improve retry detection by also checking event Kind for "retry" and recommending server-side CorrelationId to correlate attempts.
  - Add a single debug log line printing event counts for quick verification.

- Client chart code (wwwroot/timelineCharts.js)
  - Option A (minimal): preserve current category axis but accept millisecond labels.
  - Option B (recommended long-term): switch x-axis to Chart.js time scale and accept data points with ISO timestamps (requires changing JS parsing and invoking code).

- Server-side (ConsumerApi / ProducerApi) — recommended (requires cross-service change)
  - Ensure timeline events include:
	- ISO timestamps with milliseconds (e.g., DateTimeOffset.UtcNow.ToString("o")).
	- StatusCode for error cases (map exceptions to 500).
	- CorrelationId/RequestId to link retries and attempts.
  - If server-side changes are not possible immediately, dashboard will map exception events to status 500 at display time.

Affected files
- ManagementDashboard/Components/Pages/Management.razor
- ManagementDashboard/wwwroot/timelineCharts.js
- ConsumerApi timeline endpoints and event emitters (files not present in this repo view — server changes recommended; list exact files after inspecting ConsumerApi project).

Step-by-step sequence
1. (Local inspection) Pull the latest timeline payload:
   - Call ConsumerApi `timeline?minutes=30` and save sample JSON.
   - Confirm timestamp format, Kind, StatusCode, Detail shapes and presence of CorrelationId.
2. (Dashboard changes — minimal)
   - Update Management.razor RefreshTimeline:
	 - Use TimestampUtc.ToString("HH:mm:ss.fff") for labels when using string labels.
	 - Include producer events that have either StatusCode or Detail (map null StatusCode => 500).
	 - Improve retry detection: check Kind contains "retry" (case-insensitive) and Detail contains "retry".
	 - Add a Console.WriteLine showing counts for consumerEvents / producerEvents / circuitPoints.
   - Run and validate charts render and show more granular ticks.
3. (JS changes — optional/recommended)
   - Option A (quick): no JS changes; ensure timelineCharts.js renders the provided millisecond labels correctly (category axis supports fine labels).
   - Option B (recommended): refactor timelineCharts.js to use Chart.js time axis:
	 - Accept arrays of { x: ISODateString, y: number } or scatter/line data, update scales.x.type = 'time'.
	 - Update Management.razor to send ISO strings or objects via JS interop.
4. (Server-side improvements — recommended)
   - Update timeline event emitter(s) to include ISO timestamps with milliseconds, always include StatusCode for error cases, and add CorrelationId for request grouping.
   - Deploy server changes and re-generate sample payloads.
5. Validate and iterate:
   - Check that multiple events within same second are visually separated.
   - Verify producer errors appear in red (>=500) and are plotted.
   - Confirm retries are flagged where expected and that initial attempts vs retries match counts.
   - Remove debug logs when stable.

Risks
- Server-side changes require coordination with other teams or projects and versioning of the timeline API.
- Changing to Chart.js time axis requires changes to data shape and might be slightly larger change, but yields correct spacing.
- Mapping null StatusCode -> 500 client-side may hide semantics if server intends to communicate different error details; prefer server-side explicit codes if possible.

Validation steps
- Manual: Use "Refresh Timeline" on Management UI and verify:
  - Circuit chart shows finer granularity with millisecond labels or accurate time spacing.
  - Producer chart includes error points (red) for simulated exceptions.
  - Consumer attempts chart shows initial attempts and retries consistent with test runs (run Test Consumer API with several successive calls).
- Automated: Add unit/integration tests (outside scope of this plan) to assert timeline endpoint returns ISO timestamps and includes CorrelationId/StatusCode.

Notes / next actions
- I could produce a minimal PR implementing the dashboard-side C# changes (Management.razor) and a small JS option A change if you want. For full robustness, coordinate server-side changes in ConsumerApi to emit required fields.

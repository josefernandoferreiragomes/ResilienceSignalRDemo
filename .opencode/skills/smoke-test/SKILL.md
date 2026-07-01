---
name: smoke-test
description: Boot each app project, verify no startup errors, then stop
---

# Smoke Test Skill

## Purpose

Run each executable project to catch startup errors (DI resolution failures, missing services, configuration issues) before code reaches commit. Runs after build + unit tests, before review and governance.

## App Projects Tested

| Project | Type | Port | Verification |
|---|---|---|---|
| `PollyDemo.UI.Web` | Blazor web app | 5062 | Start, curl `/health` 200, stop |
| `PollyDemo.Auth.Api` | Minimal API | 5047 | Start, curl `/health` 200, stop |
| `PollyDemo.UI.Console` | Console app | N/A | Run with no args (shows usage → exit 0) |

## Script

`scripts/smoke-test.cmd` — PowerShell script that orchestrates all smoke tests.

## Failure Handling

- Any project that fails to start or returns a non-200 health check stops the pipeline
- Process output is printed for debugging
- All spawned processes are killed before the script exits

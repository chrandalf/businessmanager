# Business Manager — C# Edition (Android-ready)

You were right to push this to C#.

This repository now includes a **C# simulation core** and a **graphics-based .NET MAUI app shell** designed for Android compatibility.

## Architecture

- `csharp/src/BusinessManager.Core`
  - Deterministic, seedable simulation engine.
  - Monthly ticks, quarterly board reviews, annual resets.
  - KPI computation + Enterprise Resilience Index.
  - Initiative system with short-term and delayed effects.
  - Event-driven shock model.
- `csharp/src/BusinessManager.App`
  - .NET MAUI app targeting Android + Windows.
  - Executive dashboard page.
  - `GraphicsView` trend chart renderer (`KpiTrendDrawable`) for KPI visuals.
  - "Advance Month" loop to drive gameplay interactions.
- `csharp/data/mvp_scenario.json`
  - JSON-configurable scenario with 1 company, 2 regions, 3 BUs, and event catalog.
- `csharp/tests/BusinessManager.Core.Tests`
  - Determinism + dashboard shape tests.

## Why this is Android-compatible

The app project targets:

- `net8.0-android`
- `net8.0-windows10.0.19041.0`

So it can run on Android once MAUI workloads are available in your environment.

## Build notes

From `csharp/` (with .NET SDK + MAUI workloads installed):

```bash
dotnet build BusinessManager.sln
dotnet test BusinessManager.sln
```

## Next upgrade path (recommended)

- Replace placeholder dashboard controls with card-based boardroom UI + motion transitions.
- Add initiative detail panels with uncertainty/second-order impact badges.
- Add save/load + replay timeline UI screens.
- Add touch-first interaction model for Android tablets.

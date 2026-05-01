# Kontrol

Kontrol is a Windows desktop app for **hardware monitoring**, **fan control**, and **RGB lighting management**.
It provides a single interface to monitor sensors, automate fan behavior by temperature, and manage RGB devices with reusable profiles.

The UI is built with **WPF + WPF-UI**. Sensor data comes from **LibreHardwareMonitor**, and RGB integrations are powered by **RGB.NET** (plus optional OpenRGB and native LampArray support).

## What It Does

- Reads and groups hardware sensors (temperature, fan speed, load, and related metrics)
- Controls fans in `Auto`, `ManualConstant`, `Curve`, or `Off` modes
- Assigns per-fan temperature sources, curves, and min/max speed limits
- Supports RGB profile create/save/load workflows
- Runs from the system tray with startup behavior options
- Persists user settings and control configurations

## Main Modules

The main window has 4 sections:

- **Hardware**: Live monitoring for temperatures, fan speed, and loads
- **Fan Control**: Fan discovery, assignment, curve tuning, and source mapping
- **RGB**: Device scan, color application, and profile management
- **Settings**: Theme, startup behavior, tray options, alert threshold, OpenRGB options

## Fan Control Logic (Summary)

Each fan can be configured with:

1. A **mode**: `Auto`, `ManualConstant`, `Curve`, `Off`
2. For `Curve` mode:
   - a **temperature source** (e.g. CPU max, GPU max, motherboard max)
   - a **fan curve** (default presets or custom points)
   - optional **min/max clamp** and **Zero RPM below threshold**
3. A periodic control loop that reads sensors and applies target speed

On first run, default fan sources and three baseline curves are generated automatically.

## Configuration and Data Files

Per-user configuration is stored under `%AppData%\\Kontrol`:

- `settings.json` -> app settings
- `fan_config.json` -> fan sources, curves, assignments
- `rgb_profiles\\*.json` -> saved RGB profiles

Startup and critical error logs are written to:

- `Kontrol_startup.log` (Desktop)

## Requirements

- **OS**: Windows 10 version 2004 (build 19041) or newer
- **SDK**: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- **IDE (optional)**: Visual Studio 2022+ with .NET desktop workload

Note: Hardware-level fan/RGB control may require administrator privileges and/or vendor software, depending on motherboard/device support.

## Local Development

```bash
dotnet restore
dotnet build Kontrol.sln -c Release
dotnet run --project Kontrol/Kontrol.csproj -c Release
```

## Publish

Basic x64 publish:

```bash
dotnet publish Kontrol/Kontrol.csproj -c Release -r win-x64 --self-contained false
```

Use `--self-contained true` if you want to bundle the runtime.

## Suggested GitHub Release Plan

- **v0.1.0-alpha**
  - Core fan control loop
  - RGB profile save/load
  - Settings and tray integration
- **v0.2.0-beta**
  - Fan-curve editor UX improvements
  - Better sensor mapping validation and error messaging
  - OpenRGB connection flow improvements
- **v1.0.0**
  - Packaging/release artifact standardization
  - Documentation and sample configurations
  - Stability and performance polish

## Current Status

- The repository is under active development.
- Before release, run a full build check and XAML validation.

## License

This project is licensed under [MIT](LICENSE).

## Third-Party Dependencies

- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon)
- [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)
- [RGB.NET](https://github.com/DarthAffe/RGB.NET)
- [WPF-UI](https://github.com/lepoco/wpfui)

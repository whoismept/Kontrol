# Kontrol

A Windows desktop application combining fan speed control and RGB management in one place.

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)
![WPF](https://img.shields.io/badge/UI-WPF--UI%204.2-0078D4)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2B-blue)

---

## Features

### Fan Control
- **Auto (BIOS)** — Hand control back to the BIOS
- **Fixed Speed** — Set a manual speed from 0–100% with a slider
- **Temperature Curve** — Full control via custom temperature/speed points
- **Fan Naming** — Rename fans with your own labels
- **Fan Hiding** — Hide fans you don't use from the list
- **Curve Editor** — Drag-and-drop graphical curve editor
- **Temperature Sources** — Combine multiple sensors using average, max, or min
- **Profile Import/Export** — Save and load fan profiles in JSON format

### RGB Control
- **Windows Native LampArray** — No SDK or third-party app required; built-in support from Windows 10 2004+
- **8 Vendor SDKs** — ASUS Aura, Corsair iCUE, CoolerMaster, Logitech G HUB, MSI Center, Razer Synapse, SteelSeries GG, Wooting
- **OpenRGB** — Optional; adds extra device support via its own server
- **Color Picker** — RGB sliders, HEX input field, and preset color swatches
- **Profiles** — Save and load per-device color profiles

### Hardware Monitoring
- CPU / GPU temperatures (LibreHardwareMonitor)
- Fan speeds (RPM)
- CPU / GPU load graphs
- Live mini-stats in the sidebar (temperature + max fan RPM)
- Critical temperature alerts (configurable threshold)

### Application
- Fluent Design interface (WPF-UI + Mica backdrop)
- Minimize to system tray
- Launch on Windows startup
- Dark / Light / System theme
- Language selection (English, Turkish, German, French, Spanish, and more)

---

## Requirements

| Requirement | Minimum |
|---|---|
| Windows | 10 2004 (19041) or later |
| .NET | 9.0 Runtime |
| Administrator rights | Required for fan control |

> **Note:** Fan control requires administrator privileges so LibreHardwareMonitor can write to hardware. Without them, fans are monitored in read-only mode.

---

## Installation

### Build from source

```bash
git clone https://github.com/user/kontrol.git
cd kontrol
dotnet build -c Release
```

To run:

```bash
dotnet run --project Kontrol/Kontrol.csproj
```

### Optional SDKs

The relevant vendor software must be installed for RGB devices to be detected:

| Brand | Software |
|---|---|
| ASUS | Armoury Crate / Aura Sync |
| Corsair | iCUE |
| CoolerMaster | MasterPlus+ |
| Logitech | G HUB |
| MSI | MSI Center |
| Razer | Synapse |
| SteelSeries | SteelSeries GG |
| Wooting | — (no SDK needed, direct HID) |

Devices that support the Windows LampArray protocol require no additional software.

---

## Project Structure

```
Kontrol/
├── Models/
│   ├── Fan/              # FanConfig, FanCurve, FanAssignment, CurvePoint
│   ├── AppSettings.cs
│   ├── RgbDevice.cs      # Dual-backend: RgbNet | LampArray
│   ├── RgbProfile.cs
│   └── SensorReading.cs
├── Services/
│   ├── HardwareService.cs        # LibreHardwareMonitor integration
│   ├── FanControlService.cs      # SetSpeed, SetAuto, fan discovery
│   ├── FanControllerService.cs   # Timer + curve application loop
│   ├── FanConfigService.cs       # JSON read/write
│   ├── RgbService.cs             # RGB.NET + LampArray hybrid backend
│   ├── RgbProfileService.cs
│   ├── TempSourceEvaluator.cs    # Max/Min/Avg sensor combining
│   ├── FanCurveEvaluator.cs      # Hysteresis + interpolation
│   ├── TempAlertService.cs
│   └── ThemeHelper.cs
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── FanViewModel.cs            # Hardware monitoring (live sensors)
│   ├── FanControlViewModel.cs     # Fan assignment and curve management
│   ├── FanAssignmentViewModel.cs  # Per-fan state + renaming
│   ├── RgbViewModel.cs
│   └── SettingsViewModel.cs
├── Views/
│   ├── FanView.xaml               # Hardware Monitoring page
│   ├── FanControlView.xaml        # Fan Control page
│   ├── RgbView.xaml               # RGB Control page
│   ├── SettingsView.xaml
│   └── CurveGraphEditor.cs        # Drag-and-drop curve editor (Canvas)
├── Converters/
│   └── Converters.cs              # All IValueConverter / IMultiValueConverter
└── MainWindow.xaml                # Sidebar + navigation + live mini-stats
```

---

## Technical Notes

**Fan speed range:** `SetSoftware()` does not accept 0–100 on every board. The app performs a proportional conversion based on the hardware's `MinSoftwareValue` / `MaxSoftwareValue` before sending the value.

**LampArray backend:** The `Windows.Devices.Lights.LampArray` API ships in-box from Windows 10 2004 (build 19041). The project therefore targets `net9.0-windows10.0.19041.0`; no OpenRGB or external WinRT package is needed.

**BIOS restriction:** Some motherboards lock software fan control at the firmware level. `SetSoftware()` may send the command successfully, but the hardware can silently ignore it — this is a firmware limitation, not a bug.

**Architecture:** MVVM. Services are injected into ViewModels via constructor injection. A `DispatcherTimer` drives a polling loop configurable from 500 ms to 10 s.

---

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| WPF-UI | 4.2.1 | Fluent Design, FluentWindow, ui:Card, etc. |
| CommunityToolkit.Mvvm | 8.4.2 | ObservableObject, RelayCommand, source generators |
| LibreHardwareMonitorLib | 0.9.6 | Sensor and fan read/write |
| RGB.NET.* | 3.2.0 | Vendor SDK RGB control |
| Hardcodet.NotifyIcon.Wpf | 2.0.1 | System tray support |
| Windows.Devices.Lights | built-in | LampArray (WinRT, no extra package needed) |

---

## License

MIT

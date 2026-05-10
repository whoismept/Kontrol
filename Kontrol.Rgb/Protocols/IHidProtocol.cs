using HidSharp;
using Kontrol.Rgb.Definitions;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb.Protocols;

/// <summary>
/// Her HID protokol implementasyonunun uygulaması gereken arayüz.
/// Bir protokol, belirli bir üreticinin HID komut setini kapsar.
/// </summary>
public interface IHidProtocol
{
    string ProtocolName { get; }

    /// <summary>Cihaz ilk açıldığında init paketleri gönderir (opsiyonel).</summary>
    void Initialize(HidDevice device, HidStream stream, HidDeviceDefinition def);

    /// <summary>Tüm LED'leri tek renge boyar.</summary>
    void SetColor(HidStream stream, HidDeviceDefinition def, WinColor color);

    /// <summary>Her LED'e ayrı renk gönderir.</summary>
    void SetColors(HidStream stream, HidDeviceDefinition def, WinColor[] colors);
}

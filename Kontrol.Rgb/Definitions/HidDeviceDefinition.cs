using System.Text.Json.Serialization;

namespace Kontrol.Rgb.Definitions;

/// <summary>
/// Bir HID RGB cihazının USB ve protokol tanımı.
/// JSON dosyasından yüklenir; kullanıcı kendi tanımlarını ekleyebilir.
/// </summary>
public class HidDeviceDefinition
{
    /// <summary>Tanımlayıcı slug (benzersiz olmalı).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Kullanıcıya gösterilecek cihaz adı.</summary>
    public string Name { get; set; } = string.Empty;

    public string Vendor { get; set; } = string.Empty;

    /// <summary>Motherboard, GPU, RAM, Fan, Keyboard, Mouse, Headset, Strip, vb.</summary>
    public string Type { get; set; } = "Unknown";

    /// <summary>USB Vendor ID (ondalık). Örn: 2821 = 0x0B05 (ASUS).</summary>
    public int Vid { get; set; }

    /// <summary>Eşleşen Product ID'ler (birden fazla revizyon destekler).</summary>
    public List<int> Pids { get; set; } = [];

    /// <summary>
    /// Kullanılacak protokol adı: AuraUSB | RgbFusion2 | NzxtHue2 | Raw
    /// </summary>
    public string Protocol { get; set; } = "Raw";

    /// <summary>Toplam LED sayısı (bilinmiyorsa 1).</summary>
    public int LedCount { get; set; } = 1;

    /// <summary>Zone tanımları (opsiyonel; belirtilmezse tek zone kabul edilir).</summary>
    public List<HidZoneDefinition> Zones { get; set; } = [];

    /// <summary>HID Usage Page filtresi (opsiyonel; 0 = tümü).</summary>
    public int UsagePage { get; set; }

    /// <summary>HID Usage filtresi (opsiyonel; 0 = tümü).</summary>
    public int Usage { get; set; }

    /// <summary>
    /// Doğru HID arayüzünü bulmak için beklenen MaxOutputReportLength.
    /// 0 = kontrol yok. ASUS Aura için 65, Corsair için 65, vb.
    /// Bileşik cihazlarda (birden fazla HID arayüzü) bu alan kritiktir.
    /// </summary>
    public int ExpectedOutputReportLength { get; set; }

    /// <summary>Raw protokol için özel komut tanımları.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RawProtocolDefinition? RawProtocol { get; set; }
}

public class HidZoneDefinition
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Start index of this zone in the device's flat LED buffer.</summary>
    public int LedStart { get; set; }

    /// <summary>Default LED count for this zone.</summary>
    public int LedCount { get; set; }

    /// <summary>Protocol channel index (0-indexed). Used by AuraUSB and similar protocols.</summary>
    public int Channel { get; set; }

    /// <summary>
    /// If true the LED count is user-adjustable (typical for ARGB headers whose length
    /// depends on the strip the user has connected).
    /// </summary>
    public bool Resizable { get; set; }
}

/// <summary>
/// "Raw" protokol için kullanıcı tarafından tanımlanmış HID komutları.
/// Her komut: [reportId, byte1, byte2, ...] dizisi.
/// {R}, {G}, {B} yer tutucuları renk değerleriyle değiştirilir.
/// </summary>
public class RawProtocolDefinition
{
    /// <summary>Cihaz açıldığında bir kez gönderilir (opsiyonel).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<byte>? InitCommand { get; set; }

    /// <summary>
    /// Renk komut şablonu. {R} {G} {B} token'ları renk byte'larıyla değiştirilir.
    /// Örn: [0x00, 0xB0, "{R}", "{G}", "{B}"]
    /// </summary>
    public List<string> ColorCommand { get; set; } = [];

    /// <summary>Renk komutu sonrasında gönderilecek "uygula" komutu (opsiyonel).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<byte>? ApplyCommand { get; set; }

    /// <summary>Report boyutu (varsayılan 65).</summary>
    public int ReportSize { get; set; } = 65;
}

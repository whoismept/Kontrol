namespace Kontrol.Rgb;

public interface IOpenRgbSettings
{
    bool OpenRgbEnabled { get; }
    string OpenRgbHost { get; }
    int OpenRgbPort { get; }
    string OpenRgbClientName { get; }
}

using Microsoft.UI.Xaml;

namespace Kontrol.Services;

public static class Loc
{
    public static string Get(string key, string fallback = "")
    {
        if (Application.Current?.Resources?.TryGetValue(key, out var val) == true && val is string s)
            return s;
        return fallback;
    }

    public static string Format(string key, params object[] args)
    {
        var fmt = Get(key, key);
        try { return string.Format(fmt, args); }
        catch { return fmt; }
    }

    public static void Load(string languageCode)
    {
        try
        {
            var uri = new Uri($"ms-appx:///Resources/Strings/{languageCode}.xaml");
            var dict = new ResourceDictionary { Source = uri };
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
        catch
        {
            if (languageCode != "en")
                Load("en");
        }
    }
}

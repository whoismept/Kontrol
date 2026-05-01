using System.Windows;

namespace Kontrol.Services;

public static class Loc
{
    public static string Get(string key, string fallback = "")
        => Application.Current?.TryFindResource(key) as string ?? fallback;

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
            var uri = new Uri($"pack://application:,,,/Resources/Strings/{languageCode}.xaml", UriKind.Absolute);
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

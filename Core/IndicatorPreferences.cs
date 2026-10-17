using System.Text.Json;

namespace MultiBox.Core;

public sealed record IndicatorPreferences(string Mode = "label")
{
    public static bool IsValid(string? mode) => mode is "label" or "off";
    private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MultiBox", "indicator.json");
    public static IndicatorPreferences Load()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<IndicatorPreferences>(File.ReadAllText(SettingsPath));
            // Old outline/corner preferences become the compact pill on upgrade.
            return IsValid(settings?.Mode) ? settings! : new();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    public bool Save()
    {
        if (!IsValid(Mode)) return false;
        string staging = SettingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(staging, JsonSerializer.Serialize(this));
            File.Move(staging, SettingsPath, true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
        finally { try { File.Delete(staging); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    // Fade in briefly, hold, then fade out. Moving/resizing does not restart the 1.8 s hint.
    public static byte Opacity(string mode, double elapsedMs)
    {
        if (mode != "label" || elapsedMs >= 1800) return 0;
        double enter = Math.Clamp(elapsedMs / 140, 0, 1);
        double leave = Math.Clamp((1800 - elapsedMs) / 300, 0, 1);
        return (byte)Math.Max(1, Math.Min(enter, leave) * 255);
    }
}

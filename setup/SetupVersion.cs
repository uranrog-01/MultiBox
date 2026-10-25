using System.Reflection;

namespace MultiBox.Setup;

/// <summary>Setup's own version, used for the titlebar chip and fallback display.</summary>
public static class SetupVersion
{
    public static string Current
    {
        get
        {
            try
            {
                if (Payload.IsEmbedded)
                {
                    string? v = Payload.LoadManifest().Version;
                    if (!string.IsNullOrEmpty(v)) return v;
                }
            }
            catch { }
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        }
    }
}

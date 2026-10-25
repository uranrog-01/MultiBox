using System.IO;
using System.Reflection;
using System.Text.Json;

namespace MultiBox.Setup;

public sealed class PayloadManifest
{
    public string? Version { get; set; }
    public string? Generated { get; set; }
    public long TotalBytes { get; set; }
    public List<PayloadFileEntry> Files { get; set; } = new();
}

public sealed class PayloadFileEntry
{
    public string? Path { get; set; }
    public long Bytes { get; set; }
}

/// <summary>Access to the embedded MultiBox application payload (produced by build-setup.ps1).</summary>
public static class Payload
{
    public const string ZipResourceName = "MultiBox.Payload.Zip";
    public const string ManifestResourceName = "MultiBox.Payload.Manifest";

    private static readonly Lazy<bool> _embedded = new(() =>
        Assembly.GetExecutingAssembly().GetManifestResourceInfo(ZipResourceName) is not null);

    public static bool IsEmbedded => _embedded.Value;

    public static PayloadManifest LoadManifest()
    {
        var asm = Assembly.GetExecutingAssembly();
        using Stream? stream = asm.GetManifestResourceStream(ManifestResourceName)
            ?? throw new InvalidOperationException("Payload manifest is not embedded in this build.");
        var manifest = JsonSerializer.Deserialize<PayloadManifest>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Payload manifest is empty or invalid.");
        return manifest;
    }

    public static Stream OpenZip()
    {
        var asm = Assembly.GetExecutingAssembly();
        return asm.GetManifestResourceStream(ZipResourceName)
            ?? throw new InvalidOperationException("Payload archive is not embedded in this build.");
    }

    public static string HumanSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:0.#} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:0.#} MB",
        >= 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes} B"
    };
}

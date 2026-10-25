using System.IO;

namespace MultiBox.Setup;

/// <summary>Creates .lnk shortcuts through the Windows Script Host shell, without COM interop assemblies.</summary>
public static class ShortcutCreator
{
    public static void Create(string linkPath, string targetPath, string description, string? arguments = null)
    {
        object? shell = null;
        try
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell")
                ?? throw new InvalidOperationException("WScript.Shell is unavailable.");
            shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shellType.InvokeMember(
                "CreateShortcut",
                System.Reflection.BindingFlags.InvokeMethod,
                null, shell, new object[] { linkPath })!;

            shortcut.TargetPath = targetPath;
            if (!string.IsNullOrEmpty(arguments)) shortcut.Arguments = arguments;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath) ?? "";
            shortcut.Description = description;
            shortcut.IconLocation = $"{targetPath},0";
            shortcut.Save();
            SetupLog.Info($"Shortcut created: {linkPath}");
        }
        catch (Exception ex)
        {
            SetupLog.Warn($"Shortcut failed ({linkPath}): {ex.Message}");
        }
        finally
        {
            if (shell is not null)
            {
                try { System.Runtime.InteropServices.Marshal.ReleaseComObject(shell); } catch { }
            }
        }
    }

    public static void Delete(string linkPath)
    {
        try
        {
            if (File.Exists(linkPath)) File.Delete(linkPath);
        }
        catch (Exception ex)
        {
            SetupLog.Warn($"Shortcut removal failed ({linkPath}): {ex.Message}");
        }
    }
}

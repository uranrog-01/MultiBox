using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace MultiBox.Core
{
    public class EphemeralVault : IDisposable
    {
        public string VaultId { get; }
        public string RootPath { get; }
        public string UserProfile { get; }
        public string RoamingAppData { get; }
        public string LocalAppData { get; }
        public string LocalLowAppData { get; }
        public string TempDir { get; }
        public string UserDataDir { get; }

        private bool _isWiped;
        private readonly string _basePath;
        private FileStream? _lease;

        public EphemeralVault(string? vaultsRoot = null)
        {
            VaultId = Guid.NewGuid().ToString("N")[..12];
            _basePath = Path.GetFullPath(vaultsRoot ?? VaultRecovery.DefaultRoot);
            RootPath = Path.Combine(_basePath, VaultId);
            Directory.CreateDirectory(RootPath);
            _lease = new FileStream(Path.Combine(RootPath, VaultRecovery.LeaseFile), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            using var ownerProcess = Process.GetCurrentProcess();
            var owner = new VaultOwner(1, VaultId, Environment.ProcessId, ownerProcess.StartTime.ToUniversalTime().Ticks);
            File.WriteAllText(Path.Combine(RootPath, VaultRecovery.OwnerFile), System.Text.Json.JsonSerializer.Serialize(owner));

            // Mirror standard Windows Profile layout inside the ephemeral vault
            UserProfile = Path.Combine(RootPath, "User");
            RoamingAppData = Path.Combine(UserProfile, "AppData", "Roaming");
            LocalAppData = Path.Combine(UserProfile, "AppData", "Local");
            LocalLowAppData = Path.Combine(UserProfile, "AppData", "LocalLow");
            TempDir = Path.Combine(LocalAppData, "Temp");
            UserDataDir = Path.Combine(LocalAppData, "UserData");

            Provision();
        }

        private void Provision()
        {
            Directory.CreateDirectory(UserProfile);
            Directory.CreateDirectory(RoamingAppData);
            Directory.CreateDirectory(LocalAppData);
            Directory.CreateDirectory(LocalLowAppData);
            Directory.CreateDirectory(Path.Combine(UserProfile, "Documents"));
            Directory.CreateDirectory(Path.Combine(UserProfile, "Desktop"));
            Directory.CreateDirectory(Path.Combine(UserProfile, "Downloads"));
            Directory.CreateDirectory(TempDir);
            Directory.CreateDirectory(UserDataDir);

            // Special isolated storage for AI tools & modern Electron engines
            Directory.CreateDirectory(Path.Combine(UserProfile, ".codex"));
            Directory.CreateDirectory(Path.Combine(UserProfile, ".openai"));
            Directory.CreateDirectory(Path.Combine(UserProfile, ".config"));
            Directory.CreateDirectory(Path.Combine(RoamingAppData, "Codex"));
            Directory.CreateDirectory(Path.Combine(RoamingAppData, "ChatGPT"));
            Directory.CreateDirectory(Path.Combine(LocalAppData, "OpenAI"));
            Directory.CreateDirectory(Path.Combine(LocalAppData, "OpenAI", "Codex"));
            Directory.CreateDirectory(Path.Combine(LocalAppData, "Google", "Chrome", "User Data"));
            Directory.CreateDirectory(Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data"));
        }

        public bool ScrubAndWipe()
        {
            if (_isWiped) return true;
            string basePath = _basePath;
            string target = Path.GetFullPath(RootPath);
            if (!string.Equals(Path.GetDirectoryName(target), basePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to wipe outside the session vault root.");
            _lease?.Dispose();
            _lease = null;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                try
                {
                    if (Directory.Exists(target))
                    {
                        // Retain ownership until profile removal succeeds, so a later startup can retry.
                        foreach (string item in Directory.EnumerateFileSystemEntries(target))
                        {
                            if (Path.GetFileName(item) is VaultRecovery.OwnerFile or VaultRecovery.LeaseFile) continue;
                            var attributes = File.GetAttributes(item);
                            if ((attributes & FileAttributes.Directory) != 0) DeleteTree(item);
                            else { if ((attributes & FileAttributes.ReparsePoint) == 0) File.SetAttributes(item, FileAttributes.Normal); File.Delete(item); }
                        }
                        File.Delete(Path.Combine(target, VaultRecovery.LeaseFile));
                        File.Delete(Path.Combine(target, VaultRecovery.OwnerFile));
                        Directory.Delete(target);
                    }
                } catch (IOException) { } catch (UnauthorizedAccessException) { }
                if (!Directory.Exists(target)) { _isWiped = true; return true; }
                Thread.Sleep(120);
            }
            return false;
        }

        internal static void DeleteTree(string path)
        {
            if (!Directory.Exists(path)) return;
            // Delete junctions as links; never descend into another session or host folder.
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) { Directory.Delete(path); return; }
            foreach (string item in Directory.EnumerateFileSystemEntries(path))
            {
                var attributes = File.GetAttributes(item);
                if ((attributes & FileAttributes.Directory) != 0) DeleteTree(item);
                else { if ((attributes & FileAttributes.ReparsePoint) == 0) File.SetAttributes(item, FileAttributes.Normal); File.Delete(item); }
            }
            File.SetAttributes(path, FileAttributes.Directory);
            Directory.Delete(path);
        }

        public void Dispose()
        {
            ScrubAndWipe();
            GC.SuppressFinalize(this);
        }

        ~EphemeralVault()
        {
            try { Dispose(); } catch { _lease?.Dispose(); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace MultiBox.Core
{
    public static class PathMigrationHelper
    {
        private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".json", ".jsonl", ".txt", ".xml", ".ini", ".cfg", ".config",
            ".yml", ".yaml", ".env", ".properties", ".js", ".ts", ".html", ".log"
        };

        private static readonly HashSet<string> DatabaseExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".sqlite", ".db", ".sqlite3"
        };

        public static void EnsureDirectoryJunction(string junctionPath, string targetPath)
        {
            try
            {
                junctionPath = Path.GetFullPath(junctionPath).TrimEnd('\\', '/');
                targetPath = Path.GetFullPath(targetPath).TrimEnd('\\', '/');

                if (string.Equals(junctionPath, targetPath, StringComparison.OrdinalIgnoreCase))
                    return;

                if (!Directory.Exists(targetPath))
                    return;

                if (Directory.Exists(junctionPath))
                {
                    var attr = File.GetAttributes(junctionPath);
                    if ((attr & FileAttributes.ReparsePoint) != 0)
                    {
                        // Already a junction
                        return;
                    }
                    // If it is an empty directory, we can replace it with a junction
                    if (!Directory.EnumerateFileSystemEntries(junctionPath).GetEnumerator().MoveNext())
                    {
                        try { Directory.Delete(junctionPath); } catch { return; }
                    }
                    else
                    {
                        return; // Non-empty real directory, do not destroy
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(junctionPath)!);
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c mklink /J \"{junctionPath}\" \"{targetPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                proc?.WaitForExit(3000);
            }
            catch
            {
                // Best-effort junction alias
            }
        }

        public static void MigrateVaultPaths(string targetDataDir, string oldVaultPath, string newProfilePath)
        {
            try
            {
                if (!Directory.Exists(targetDataDir)) return;

                oldVaultPath = Path.GetFullPath(oldVaultPath).TrimEnd('\\', '/');
                newProfilePath = Path.GetFullPath(newProfilePath).TrimEnd('\\', '/');

                if (string.Equals(oldVaultPath, newProfilePath, StringComparison.OrdinalIgnoreCase))
                    return;

                string oldWin = oldVaultPath;
                string newWin = newProfilePath;

                string oldUnix = oldVaultPath.Replace('\\', '/');
                string newUnix = newProfilePath.Replace('\\', '/');

                string oldEscaped = oldVaultPath.Replace("\\", "\\\\");
                string newEscaped = newProfilePath.Replace("\\", "\\\\");

                var dirInfo = new DirectoryInfo(targetDataDir);
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    if ((file.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                    if (file.Length > 20 * 1024 * 1024) continue; // Skip huge binaries

                    string ext = file.Extension;
                    if (DatabaseExtensions.Contains(ext))
                    {
                        MigrateSqliteDatabase(file.FullName, oldWin, newWin, oldUnix, newUnix);
                    }
                    else if (TextExtensions.Contains(ext) || file.Length < 1024 * 1024)
                    {
                        MigrateTextFile(file.FullName, oldWin, newWin, oldUnix, newUnix, oldEscaped, newEscaped);
                    }
                }
            }
            catch
            {
                // Migration is best-effort; app files are never deleted on failure
            }
        }

        public static void MigrateAnyOldVaultPaths(string targetDataDir, string newProfilePath)
        {
            try
            {
                if (!Directory.Exists(targetDataDir)) return;
                newProfilePath = Path.GetFullPath(newProfilePath).TrimEnd('\\', '/');
                string newWin = newProfilePath;
                string newUnix = newProfilePath.Replace('\\', '/');
                string newEscaped = newProfilePath.Replace("\\", "\\\\");

                // Pattern to match any C:\...\AppData\Local\MultiBox\Vaults\<12-hex-id>
                var regex = new Regex(@"([a-zA-Z]:[\\/][^""'\r\n]+[\\/]AppData[\\/]Local[\\/]MultiBox[\\/]Vaults[\\/])([a-fA-F0-9]{12})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                var dirInfo = new DirectoryInfo(targetDataDir);
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    if ((file.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                    if (file.Length > 20 * 1024 * 1024) continue;

                    string ext = file.Extension;
                    if (TextExtensions.Contains(ext) || file.Length < 512 * 1024)
                    {
                        try
                        {
                            string content = File.ReadAllText(file.FullName);
                            var matches = regex.Matches(content);
                            if (matches.Count > 0)
                            {
                                foreach (Match m in matches)
                                {
                                    string matchedOld = m.Value.TrimEnd('\\', '/');
                                    // Ensure junction for backward compatibility
                                    EnsureDirectoryJunction(matchedOld, newProfilePath);

                                    string mWin = matchedOld.Replace('/', '\\');
                                    string mUnix = matchedOld.Replace('\\', '/');
                                    string mEsc = mWin.Replace("\\", "\\\\");

                                    content = content.Replace(mWin, newWin)
                                                     .Replace(mUnix, newUnix)
                                                     .Replace(mEsc, newEscaped);
                                }
                                File.WriteAllText(file.FullName, content);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static void MigrateTextFile(string path, string oldWin, string newWin, string oldUnix, string newUnix, string oldEscaped, string newEscaped)
        {
            try
            {
                string text = File.ReadAllText(path);
                if (text.Contains(oldWin, StringComparison.OrdinalIgnoreCase) ||
                    text.Contains(oldUnix, StringComparison.OrdinalIgnoreCase) ||
                    text.Contains(oldEscaped, StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Replace(oldWin, newWin, StringComparison.OrdinalIgnoreCase)
                               .Replace(oldUnix, newUnix, StringComparison.OrdinalIgnoreCase)
                               .Replace(oldEscaped, newEscaped, StringComparison.OrdinalIgnoreCase);

                    File.WriteAllText(path, text);
                }
            }
            catch { }
        }

        private static void MigrateSqliteDatabase(string dbPath, string oldWin, string newWin, string oldUnix, string newUnix)
        {
            try
            {
                var builder = new SqliteConnectionStringBuilder
                {
                    DataSource = dbPath,
                    Mode = SqliteOpenMode.ReadWrite,
                    Pooling = false,
                    DefaultTimeout = 3
                };

                using var conn = new SqliteConnection(builder.ToString());
                conn.Open();

                var tables = new List<string>();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }

                foreach (string table in tables)
                {
                    var textColumns = new List<string>();
                    using (var colCmd = conn.CreateCommand())
                    {
                        colCmd.CommandText = $"PRAGMA table_info(\"{table}\");";
                        using var colReader = colCmd.ExecuteReader();
                        while (colReader.Read())
                        {
                            string colName = colReader.GetString(1);
                            string colType = colReader.GetString(2).ToUpperInvariant();
                            if (colType.Contains("TEXT") || colType.Contains("CHAR") || colType.Contains("CLOB") || string.IsNullOrWhiteSpace(colType))
                            {
                                textColumns.Add(colName);
                            }
                        }
                    }

                    if (textColumns.Count == 0) continue;

                    using var tx = conn.BeginTransaction();
                    try
                    {
                        foreach (string col in textColumns)
                        {
                            using var updateCmd = conn.CreateCommand();
                            updateCmd.Transaction = tx;
                            updateCmd.CommandText = $"UPDATE \"{table}\" SET \"{col}\" = replace(replace(\"{col}\", @oldWin, @newWin), @oldUnix, @newUnix) WHERE \"{col}\" LIKE @searchWin OR \"{col}\" LIKE @searchUnix;";
                            updateCmd.Parameters.AddWithValue("@oldWin", oldWin);
                            updateCmd.Parameters.AddWithValue("@newWin", newWin);
                            updateCmd.Parameters.AddWithValue("@oldUnix", oldUnix);
                            updateCmd.Parameters.AddWithValue("@newUnix", newUnix);
                            updateCmd.Parameters.AddWithValue("@searchWin", $"%{oldWin}%");
                            updateCmd.Parameters.AddWithValue("@searchUnix", $"%{oldUnix}%");
                            updateCmd.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                    }
                }

                conn.Close();
            }
            catch { }
        }
    }
}


using osu.Framework;
using WebSocketSharp;
using Logger = osu.Framework.Logging.Logger;

namespace osu.Game.Rulesets.UMania;

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;
public static class UbSteamDirectoryFinder
{

    public static string? FindUnbeatableUserPackages()
    {
        string? root = FindUnbeatableRoot();
        if (root != null)
        {
            string packagesPath = Path.Combine(root, "USER_PACKAGES");
            if (Directory.Exists(packagesPath))
            {
                return packagesPath;
            }
            
            Directory.CreateDirectory(packagesPath);
            return packagesPath;
        }
        return null; // Packages directory not found
    }
    
    public static string? FindUnbeatableRoot()
    {
        List<string> possibleRoots = new List<string>();
        
        if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
        {
            // Windows
            try
            {
                string? steamPath = GetSteamInstallPathWindows();
                if (!string.IsNullOrEmpty(steamPath))
                {
                    List<string> libraryPaths = GetSteamLibraryPaths(steamPath);
                    foreach (string p in libraryPaths)
                    {
                        possibleRoots.Add(Path.Combine(p, "steamapps", "common", "UNBEATABLE"));
                        possibleRoots.Add(Path.Combine(p, "steamapps", "common", "UNBEATABLE [white label]"));
                    }
                }
            }
            catch
            {
                Logger.Log("Failed to find Steam directory from windows registry");
            }
        }
        else if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
        {
            try
            {
                string steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local", "share", "Steam");
                if (Directory.Exists(steamPath))
                {
                    List<string> libraryPaths = GetSteamLibraryPaths(steamPath);
                    foreach (string p in libraryPaths)
                    {
                        possibleRoots.Add(Path.Combine(p, "steamapps", "common", "UNBEATABLE"));
                        possibleRoots.Add(Path.Combine(p, "steamapps", "common", "UNBEATABLE [white label]"));
                    }
                }
            }
            catch
            {
                Logger.Log("Failed to find Steam directory from unix system");
            }
        }

        foreach (string root in possibleRoots)
        {
            if (Directory.Exists(root))
                return root;
        }

        return null; // Game not found
    }

    private static string? GetSteamInstallPathWindows()
    {
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Valve\Steam"))
        {
            if (key != null)
                return key.GetValue("InstallPath") as string;
        }
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
        {
            if (key != null)
                return key.GetValue("SteamPath") as string;
        }
        return null;
    }

    private static List<string> GetSteamLibraryPaths(string steamPath)
    {
        List<string> paths = new List<string> { steamPath };
        string libraryFoldersFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (File.Exists(libraryFoldersFile))
        {
            string[] lines = File.ReadAllLines(libraryFoldersFile);
            foreach (string line in lines)
            {
                if (line.Contains("\"path\""))
                {
                    string path = line.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries)[3];
                    path = path.Replace("\\\\", "\\");
                    paths.Add(path);
                }
            }
        }
        return paths;
    }
}

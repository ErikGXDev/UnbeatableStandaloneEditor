using System.Reflection;

namespace UnbeatableStandaloneEditor;

public static class AppVersion
{
    public static string Current { get; } = GetVersion();

    private static string GetVersion()
    {
        var version = typeof(AppVersion).Assembly.GetName().Version;
        if (version == null)
            return "1.0.0";

        // Format as semantic version (e.g., "1.0.0")
        return $"{version.Major}.{version.Minor}.{version.Build}";
    }


    public static int Compare(string versionA, string versionB)
    {
        var a = ParseVersion(versionA);
        var b = ParseVersion(versionB);

        if (a.Major != b.Major)
            return a.Major.CompareTo(b.Major);
        if (a.Minor != b.Minor)
            return a.Minor.CompareTo(b.Minor);
        return a.Patch.CompareTo(b.Patch);
    }

    private static (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        // Remove 'v' prefix if present
        version = version.TrimStart('v', 'V');

        var parts = version.Split('.');
        if (parts.Length < 3)
        {
            // Pad with zeros if needed
            var list = new List<string>(parts);
            while (list.Count < 3)
                list.Add("0");
            parts = list.ToArray();
        }

        int.TryParse(parts[0], out int major);
        int.TryParse(parts[1], out int minor);
        int.TryParse(parts[2], out int patch);

        return (major, minor, patch);
    }
}

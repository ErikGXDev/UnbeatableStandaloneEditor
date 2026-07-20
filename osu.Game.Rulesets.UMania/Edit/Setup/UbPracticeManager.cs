using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace osu.Game.Rulesets.UMania.Edit.Setup;

public class UbPracticeManager
{
    private const string practice_entry_tag = "UnbeatableStandaloneEditor";

    public static string GetPracticeModeSettingsPath() => Path.Combine(UbExportSection.GetDataDirectory(), "practice-mode-settings.txt");
    
    public static void WritePracticeEntry(string title, int startTimeMs)
    {
        string path = GetPracticeModeSettingsPath();

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        List<string> lines = File.Exists(path)
            ? File.ReadAllLines(path).ToList()
            : new List<string>();

        RemovePracticeEntryInternal(lines);

        lines.Add($"// >>> {practice_entry_tag} practice entry");
        lines.Add($"{title.ToUpper()}:{startTimeMs}");
        lines.Add($"// <<< {practice_entry_tag} end");

        File.WriteAllText(path, string.Join("\r\n", lines) + "\r\n");
    }

    public static void RemovePracticeEntry()
    {
        string path = GetPracticeModeSettingsPath();

        if (!File.Exists(path))
            return;

        var lines = File.ReadAllText(path).Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').ToList();
        RemovePracticeEntryInternal(lines);
        File.WriteAllText(path, string.Join("\r\n", lines) + "\r\n");
    }

    private static void RemovePracticeEntryInternal(List<string> lines)
    {
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            if (lines[i].Contains(practice_entry_tag))
            {
                int removeCount = Math.Min(3, lines.Count - i);
                lines.RemoveRange(i, removeCount);
            }
        }
    }
}
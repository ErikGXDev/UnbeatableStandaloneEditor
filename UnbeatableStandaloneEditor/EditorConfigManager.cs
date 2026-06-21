using osu.Framework.Configuration;
using osu.Framework.Platform;
using UnbeatableStandaloneEditor.BeatmapPicker;

namespace UnbeatableStandaloneEditor;

public class EditorConfigManager : IniConfigManager<EditorSetting>
{
    protected override string Filename => "editor.ini";

    public EditorConfigManager(Storage storage) : base(storage) { }

    protected override void InitialiseDefaults()
    {
        SetDefault(EditorSetting.FirstLaunch, true);
        SetDefault(EditorSetting.ShowSystemCursor, true);
        SetDefault(EditorSetting.SortMode, SortMode.Artist);
    }
}

public enum EditorSetting
{
    FirstLaunch,
    ShowSystemCursor,
    SortMode
}

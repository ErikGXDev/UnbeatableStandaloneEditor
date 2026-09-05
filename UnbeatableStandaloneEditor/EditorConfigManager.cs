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
        //SetDefault(EditorSetting.UseAutoUpdater, false); // Old, never use
        SetDefault(EditorSetting.NewDisableUpdater, false);
    }
}

public enum EditorSetting
{
    FirstLaunch,
    ShowSystemCursor,
    SortMode,
    //UseAutoUpdater, // Old, never use
    NewDisableUpdater
}

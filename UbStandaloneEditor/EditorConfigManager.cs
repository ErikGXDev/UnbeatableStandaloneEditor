using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace UbStandaloneEditor;

public class EditorConfigManager : IniConfigManager<EditorSetting>
{
    protected override string Filename => "editor.ini";

    public EditorConfigManager(Storage storage) : base(storage) { }

    protected override void InitialiseDefaults()
    {
        SetDefault(EditorSetting.FirstLaunch, true);
    }
}

public enum EditorSetting
{
    FirstLaunch,
}

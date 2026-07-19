using osu.Framework;

namespace osu.Game.Rulesets.UMania.Edit.Setup;

public static class UbPlatform
{
    public static bool IsWindows() => RuntimeInfo.OS == RuntimeInfo.Platform.Windows;

    public static bool IsLinux() => RuntimeInfo.OS == RuntimeInfo.Platform.Linux;

    public static bool IsMacOS() => RuntimeInfo.OS == RuntimeInfo.Platform.macOS;
}

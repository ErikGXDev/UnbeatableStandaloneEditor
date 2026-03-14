using osu.Framework;

namespace UnbeatableStandaloneEditor;

class Program
{
    static void Main(string[] args)
    {
        using var host = Host.GetSuitableDesktopHost("unbeatable-beatmap-editor", new HostOptions()
        {
            PortableInstallation = false,
            FriendlyGameName = "UNBEATABLE Standalone Beatmap Editor"
        });
        host.Run(new MainGame());
    }
}

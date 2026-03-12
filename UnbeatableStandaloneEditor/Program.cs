using osu.Framework;

namespace UnbeatableStandaloneEditor;

class Program
{
    static void Main(string[] args)
    {
        using var host = Host.GetSuitableDesktopHost("unbeatable-beatmap-editor", new HostOptions()
        {
            PortableInstallation = false,
            FriendlyGameName = "unbeatable standalone beatmap editor"
        });
        host.Run(new MainGame());
    }
}

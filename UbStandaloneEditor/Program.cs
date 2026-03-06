namespace UbStandaloneEditor;
       
using osu.Framework;
using osu.Framework.Platform;

class Program
{
    static void Main(string[] args)
    {
        using var host = Host.GetSuitableDesktopHost("unbeatable-beatmap-editor", new HostOptions()
        {
            PortableInstallation = true,
            FriendlyGameName = "unbeatable beatmap editor"
        });
        host.Run(new MainGame());
    }
}

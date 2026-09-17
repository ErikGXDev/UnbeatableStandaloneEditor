using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osuTK;

namespace UnbeatableStandaloneEditor.Update;

public partial class UpdateButton : RoundedButton
{
    public UpdateButton()
    {

    }

    [Resolved] private EditorConfigManager config { get; set; } = null!;
    [Resolved] private OverlayColourProvider colours { get; set; } = null!;

    [BackgroundDependencyLoader]
    private void load()
    {
        var useUpdater = true;

        Anchor = Anchor.TopRight;
        Origin = Anchor.TopRight;
        Width = 180;
        Height = 28;
        Scale = new Vector2(0f);
        Y = 30;
        Colour = colours.Colour1;
        BackgroundColour = useUpdater ? colours.Colour4 : colours.Background3;
        Text = useUpdater ? "Download new version!" : "New version available!";
    }

    // Weird hack because config bindables dont work for some reason
    private double lastUpdateCheck = 0;
    protected override void Update()
    {
        base.Update();

        // Check if a second has passed
        if (Time.Current - lastUpdateCheck > 1000)
        {
            lastUpdateCheck = Time.Current;

            var useUpdater = true;

            BackgroundColour = useUpdater ? colours.Colour4 : colours.Background3;
            Text = useUpdater ? "Download new version!" : "New version available!";
        }
    }
}

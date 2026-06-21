using osu.Framework.Allocation;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;

namespace UnbeatableStandaloneEditor.Components;

public partial class BlankButton : OsuButton
{
    public BlankButton() {}

    [Resolved]
    private OverlayColourProvider colourProvider { get; set; } = null!;

    [BackgroundDependencyLoader]
    private void load()
    {
        Colour = colourProvider.Colour1;
        BackgroundColour = colourProvider.Background3;

        Content.CornerRadius = 8;
    }
}

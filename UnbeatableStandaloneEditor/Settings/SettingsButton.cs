using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using UnbeatableStandaloneEditor.Components;

namespace UnbeatableStandaloneEditor.Settings;

public partial class SettingsButton : BlankButton, IHasPopover // Didnt want the triangles, so OsuButton only
{
    public SettingsButton() {}

    [Resolved]
    private OverlayColourProvider colourProvider { get; set; } = null!;

    [BackgroundDependencyLoader]
    private void load()
    {
        Origin = Anchor.TopRight;
        Anchor = Anchor.TopRight;
        Size = new Vector2(32);


        // Note to self: Do not use Child = ... because it removes the background of the button for some reason.
        Add(new SpriteIcon
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Size = new Vector2(16),
            Icon = FontAwesome.Solid.Cog,
            Depth = -1,
        });

        Action = () =>
        {
            this.ShowPopover();
        };
    }

    public Popover GetPopover() => new SettingsPopover();
}

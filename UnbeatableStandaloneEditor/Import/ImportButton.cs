using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using UnbeatableStandaloneEditor.Components;

namespace UnbeatableStandaloneEditor.Import;

public partial class ImportButton : BlankButton, IHasPopover
{
    [Resolved]
    private OverlayColourProvider colourProvider { get; set; } = null!;

    [BackgroundDependencyLoader]
    private void load()
    {
        Origin = Anchor.TopRight;
        Anchor = Anchor.TopRight;
        Size = new Vector2(70, 32);


        Text = "Import";

        Action = () =>
        {
            this.ShowPopover();
        };
    }

    public Popover GetPopover() => new ImportPopover();
}

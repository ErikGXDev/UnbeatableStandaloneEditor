using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;

namespace UnbeatableStandaloneEditor.Settings;

public partial class MenuPopoverContainer : PopoverContainer
{
    public MenuPopoverContainer() {}

    [BackgroundDependencyLoader]
    private void load()
    {
        RelativeSizeAxes = Axes.Both;
    }
}

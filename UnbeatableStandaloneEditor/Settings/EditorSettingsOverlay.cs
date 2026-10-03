using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Cursor;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;

namespace UnbeatableStandaloneEditor.Settings;

public partial class EditorSettingsOverlay : SettingsPanel
{
    protected override IEnumerable<SettingsSection> CreateSections()
    {
        return new List<SettingsSection>
        {
            new GeneralSettingsSection(),
            new AdvancedSettingsSection(),
            new VolumeSettingsSection(),
            new BackupSettingsSection(),
            new WebsocketSettingsSection(),
            new KeyBindingsSettingsSection(),
        };
    }

    protected override Drawable CreateHeader() => new EditorSettingsHeader();

    protected override Drawable CreateFooter() => new OsuContextMenuContainer
    {
        RelativeSizeAxes = Axes.X,
        AutoSizeAxes = Axes.Y,
        Child = new EditorSettingsFooter(),
    };

    public EditorSettingsOverlay()
        : base(false)
    {
    }

    [Cached]
    private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

    protected override void LoadComplete()
    {
        base.LoadComplete();

        foreach (var drawable in ContentContainer.Children)
        {
            if (drawable is Box box)
                box.Colour = colourProvider.Background4;
        }
    }
}

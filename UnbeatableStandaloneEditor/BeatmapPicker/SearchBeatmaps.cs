using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using UnbeatableStandaloneEditor.Components;

namespace UnbeatableStandaloneEditor.BeatmapPicker;

public partial class SearchBeatmaps : SearchTextBox
{
    [Resolved]
    private OverlayColourProvider colours { get; set; } = null!;

    private Bindable<string> searchQuery = new Bindable<string>();

    public SearchBeatmaps(Bindable<string> searchQuery)
    {
        this.searchQuery = searchQuery;
        Anchor = Anchor.CentreRight;
        Origin = Anchor.CentreRight;
        Width = 148;
        Height = 32;
        PlaceholderText = "Search...";

        Current = searchQuery;
    }

    [BackgroundDependencyLoader]
    private void load()
    {

    }

    protected override void LoadComplete()
    {
        BackgroundFocused = colours.Background4;
        BackgroundUnfocused = colours.Background4;

        CornerRadius = 8;

        base.LoadComplete();
    }
}

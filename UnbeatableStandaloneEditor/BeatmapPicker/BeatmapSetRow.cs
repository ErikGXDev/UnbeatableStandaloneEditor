using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace UnbeatableStandaloneEditor.BeatmapPicker;


public partial class BeatmapSetRow : OsuClickableContainer
{
    private readonly BeatmapSetInfo set;
    private readonly Bindable<BeatmapSetInfo?> selectedSet;

    private Box selectionOverlay = null!;
    private Box hoverOverlay = null!;
    private Box leftAccent = null!;

    public BeatmapSetRow(BeatmapSetInfo set, Bindable<BeatmapSetInfo?> selectedSet)
    {
        this.set = set;
        this.selectedSet = selectedSet;

        RelativeSizeAxes = Axes.X;
        Height = 56;
        Masking = true;
        CornerRadius = 5;
    }

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colours)
    {
        string diffLabel = set.Beatmaps.Count == 1 ? "1 difficulty" : $"{set.Beatmaps.Count} difficulties";

        Action = () => selectedSet.Value = set;

        Children =
        [
            new Box { RelativeSizeAxes = Axes.Both, Colour = colours.Background3 },
            selectionOverlay = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = colours.Highlight1,
                Alpha = 0,
                AlwaysPresent = true,
            },
            hoverOverlay = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.White.Opacity(0.05f),
                Alpha = 0,
                AlwaysPresent = true,
            },
            leftAccent = new Box
            {
                Width = 3,
                RelativeSizeAxes = Axes.Y,
                Colour = colours.Highlight1,
                Alpha = 0,
                AlwaysPresent = true,
            },
            new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding { Left = 14, Right = 12, Vertical = 6 },
                Child = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 2),
                    Children =
                    [
                        new OsuSpriteText
                        {
                            Text = $"{set.Metadata.Artist} \u2014 {set.Metadata.Title}",
                            Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold),
                        },
                        new OsuSpriteText
                        {
                            Text = $"by {set.Metadata.Author.Username}  \u2022  {diffLabel}",
                            Font = OsuFont.GetFont(size: 14),
                            Alpha = 0.55f,
                        }
                    ],
                },
            }
        ];
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();
        selectedSet.BindValueChanged(onSelectionChanged, true);
    }

    private void onSelectionChanged(ValueChangedEvent<BeatmapSetInfo?> e)
    {
        bool isSelected = e.NewValue?.ID == set.ID;
        selectionOverlay.FadeTo(isSelected ? 0.18f : 0f, 100, Easing.OutQuint);
        leftAccent.FadeTo(isSelected ? 1f : 0f, 100, Easing.OutQuint);
    }

    protected override bool OnHover(HoverEvent e)
    {
        hoverOverlay.FadeIn(60);
        return base.OnHover(e);
    }

    protected override void OnHoverLost(HoverLostEvent e)
    {
        hoverOverlay.FadeOut(60);
        base.OnHoverLost(e);
    }

    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);
        if (isDisposing)
            selectedSet.ValueChanged -= onSelectionChanged;
    }
}

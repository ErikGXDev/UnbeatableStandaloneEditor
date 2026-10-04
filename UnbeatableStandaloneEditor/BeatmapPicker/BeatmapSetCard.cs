using Humanizer;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace UnbeatableStandaloneEditor.BeatmapPicker;

public partial class BeatmapSetCard : OsuClickableContainer
{
    private readonly BeatmapSetInfo set;
    private readonly Bindable<BeatmapSetInfo?> selectedSet;

    [Resolved] private BeatmapManager beatmapManager { get; set; } = null!;

    private Box selectionOverlay = null!;
    private Box hoverOverlay = null!;
    private Box leftAccent = null!;

    public BeatmapSetCard(BeatmapSetInfo set, Bindable<BeatmapSetInfo?> selectedSet)
    {
        this.set = set;
        this.selectedSet = selectedSet;
    }

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colours)
    {
        Action = () => selectedSet.Value = set;

        string diffLabel = set.Beatmaps.Count == 1 ? "1 difficulty" : $"{set.Beatmaps.Count} difficulties";

        RelativeSizeAxes = Axes.Both;

        AlwaysPresent = true;

        Padding = new MarginPadding { Right = 8, Bottom = 8 };

        var working = beatmapManager.GetWorkingBeatmap(set.Beatmaps.FirstOrDefault());
        var background = working.GetBackground();
        var hasBackground = background != null;

        Children =
        [
            new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 5,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = colours.Background3 },
                    hasBackground
                        ? new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            CornerRadius = 5,
                            Children = new Drawable[]
                            {
                                new BeatmapSetRow.RowBackgroundSprite(background)
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    FillMode = FillMode.Fill,
                                    Alpha = 1f,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Scale = new Vector2(0.99f),
                                    EdgeSmoothness = new Vector2(2),
                                },
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.Black.Opacity(0.02f),
                                    Scale = new Vector2(1.05f)
                                },
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.BottomCentre,
                                    Height = 1f,
                                    Colour = ColourInfo.GradientVertical(
                                        colours.Background3.Opacity(0), colours.Background3),
                                    Scale = new Vector2(1.05f)
                                },
                                /*new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.BottomCentre,
                                    Height = 0.75f,
                                    Colour = ColourInfo.GradientVertical(
                                        colours.Background3.Opacity(0), colours.Background3),
                                    AlwaysPresent = true,
                                },*/
                            }
                        }
                        : Empty(),
                    selectionOverlay = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colours.Highlight1,
                        Alpha = 0,
                    },
                    hoverOverlay = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White.Opacity(0.05f),
                        Alpha = 0,
                    },
                    leftAccent = new Box
                    {
                        Width = 3,
                        RelativeSizeAxes = Axes.Y,
                        Colour = colours.Highlight1,
                        Alpha = 0,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Left = 8, Right = 8, Bottom = 6 },
                        Child = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 2),
                            Children =
                            [
                                new OsuTextFlowContainer(t =>
                                {
                                    t.Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold);
                                    t.Shadow = true;
                                    t.ShadowColour = Color4.Black.Opacity(0.25f);
                                    t.ShadowOffset = new Vector2(0, 0.07f);
                                })
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Text = $"{set.Metadata.Artist} \u2014 {set.Metadata.Title}".Truncate(250)
                                },
                                new OsuTextFlowContainer(t =>
                                {
                                    t.Font = OsuFont.GetFont(size: 12);
                                })
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    ParagraphSpacing = 0.2f,
                                    Text =
                                        $"by {set.Metadata.Author.Username}\n{diffLabel}  \u2022  {HumanizerUtils.Humanize(set.DateAdded)}"
                                            .Truncate(250),
                                    Alpha = 0.75f,
                                }
                            ],
                        },
                    },
                }
            },
        ];
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();
        selectedSet.BindValueChanged(onSelectionChanged, true);
    }

    private void onSelectionChanged(ValueChangedEvent<BeatmapSetInfo?> e)
    {
        //Logger.Log($"Selection changed to {e.NewValue?.ID} (current set ID: {set.ID})");
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
        if (isDisposing)
            selectedSet.ValueChanged -= onSelectionChanged;

        base.Dispose(isDisposing);
    }
}

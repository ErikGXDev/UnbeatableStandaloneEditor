using Humanizer;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace UnbeatableStandaloneEditor.BeatmapPicker;


public partial class BeatmapSetRow : OsuClickableContainer
{
    private readonly BeatmapSetInfo set;
    private readonly Bindable<BeatmapSetInfo?> selectedSet;

    [Resolved]
    private BeatmapManager beatmapManager { get; set; } = null!;

    private Box selectionOverlay = null!;
    private Box hoverOverlay = null!;
    private Box leftAccent = null!;

    private Action doubleClick = null!;

    public BeatmapSetRow(BeatmapSetInfo set, Bindable<BeatmapSetInfo?> selectedSet, Action doubleClick)
    {
        this.set = set;
        this.selectedSet = selectedSet;
        this.doubleClick = doubleClick;
    }

    private double lastClickTime;

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colours)
    {
        string diffLabel = set.Beatmaps.Count == 1 ? "1 difficulty" : $"{set.Beatmaps.Count} difficulties";

        Action = () =>
        {
            selectedSet.Value = set;

            if (Time.Current - lastClickTime < 200)
            {
                //doubleClick.Invoke();
            }

            lastClickTime = Time.Current;
        };

        RelativeSizeAxes = Axes.X;
        Height = 56;
        Masking = true;
        CornerRadius = 5;

        var working = beatmapManager.GetWorkingBeatmap(set.Beatmaps.FirstOrDefault());
        var background = working.GetBackground();
        var hasBackground = background != null;

        Children =
        [
            new Box { RelativeSizeAxes = Axes.Both, Colour = colours.Background3 },
            hasBackground ? new Container
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                Width = 0.42f,
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 55,
                        Origin = Anchor.CentreLeft,
                        Anchor = Anchor.CentreLeft,
                        CornerRadius = 5,
                        Masking = true,
                        Child = new RowBackgroundSprite(background)
                        {
                            Y = -4,
                            RelativeSizeAxes = Axes.Both,
                            Origin = Anchor.CentreLeft,
                            FillMode = FillMode.Fill,
                            Alpha = 1f,
                            AlwaysPresent = true,
                        }
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black.Opacity(0.08f),
                        AlwaysPresent = true,
                        Scale = new Vector2(1.05f)
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourInfo.GradientHorizontal(colours.Background3, colours.Background3.Opacity(0)),
                        AlwaysPresent = true,
                        Scale = new Vector2(1.05f)
                    },
                }
            } : Empty(),
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
                            Text = $"{set.Metadata.Artist} \u2014 {set.Metadata.Title}".Truncate(250),
                            Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold),
                        },
                        new OsuSpriteText
                        {
                            Text = $"by {set.Metadata.Author.Username}  \u2022  {diffLabel}  \u2022  {HumanizerUtils.Humanize(set.DateAdded)}".Truncate(250),
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

    public partial class RowBackgroundSprite : Sprite
    {
        private readonly Texture texture;

        public RowBackgroundSprite(Texture texture)
        {
            ArgumentNullException.ThrowIfNull(texture);

            this.texture = texture;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Texture = texture;
        }
    }
}

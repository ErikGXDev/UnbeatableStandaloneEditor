using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit;


public partial class PreviewCopIndicator : Container
{
    public const int COP_COUNT = 4;

    private readonly OsuSpriteText[] copTexts = new OsuSpriteText[COP_COUNT];

    private Colour4 disabledColor;

    public PreviewCopIndicator()
    {
    }

    [Resolved] private OverlayColourProvider overlayColourProvider { get; set; } = null!;
    [Resolved] private OsuColour colours { get; set; } = null!;

    [BackgroundDependencyLoader]
    private void load()
    {
        disabledColor = overlayColourProvider.Background1.Lighten(0.2f);

        Width = 76;
        Height = 24;
        Origin = Anchor.CentreRight;

        var textContainer = new Container
        {
            RelativeSizeAxes = Axes.Both,
            Padding = new MarginPadding { Horizontal = 11, Vertical = 10 },
        };

        for (int i = 0; i < COP_COUNT; i++)
        {
            int index = i;

            textContainer.Add(copTexts[index] = new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Position = new Vector2(((index + 1) / (float)(COP_COUNT + 1) - 0.5f) * 88, 0),
                Text = $"{index + 1}",
                Colour = disabledColor,
                Font = OsuFont.Default.With(size: 16, weight: FontWeight.SemiBold),
            });
        }

        Children = new Drawable[]
        {
            new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 4,
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = overlayColourProvider.Background3,
                },
            },
            textContainer,
        };
    }

    public void UpdateCops(bool[] alive)
    {
        for (int i = 0; i < COP_COUNT; i++)
            copTexts[i].FadeColour(alive[i] ? Colour4.White : disabledColor, 80, Easing.OutQuint);
    }
}
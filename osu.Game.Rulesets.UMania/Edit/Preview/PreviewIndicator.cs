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

public partial class PreviewIndicator : Container
{
    public PreviewIndicator()
    {
        
    }

    [Resolved] private OverlayColourProvider overlayColourProvider { get; set; } = null!;
    [Resolved] private OsuColour colours { get; set; } = null!;

    private OsuSpriteText leftText;
    private OsuSpriteText middleText;
    private OsuSpriteText rightText;

    private Box flashBox;

    private Colour4 disabledColor;

    [BackgroundDependencyLoader]
    private void load()
    {

        disabledColor = overlayColourProvider.Background1.Lighten(0.2f);
        
        Width = 72;
        Height = 24;
        Origin = Anchor.Centre;

        Children = new Drawable[]
        {
            new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 4,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = overlayColourProvider.Background3,
                    },
                    /*new Container()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 4,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = 2,
                        Scale = new Vector2(0.9f),
                        Child = flashBox = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Colour = overlayColourProvider.Background2
                        }
                    }*/
                   
                },
            },
            new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding() {Horizontal = 11,  Vertical = 10},
                Children = new Drawable[]
                {
                    leftText = new OsuSpriteText()
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "L",
                        Colour = disabledColor,
                        Font = OsuFont.Default.With(size: 16, weight: FontWeight.SemiBold),
                    },
                    middleText = new OsuSpriteText()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = "M",
                        Colour = disabledColor,
                        Font = OsuFont.Default.With(size: 16, weight: FontWeight.SemiBold),
                    },
                    rightText = new OsuSpriteText()
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Text = "R",
                        Colour = disabledColor,
                        Font = OsuFont.Default.With(size: 16, weight: FontWeight.SemiBold),
                    },
                },
            },
        };
    }

    
    private bool prevFlippedRight;
    private bool prevInMiddle;
    
    public void UpdateIndicators(bool flippedRight, bool inMiddle)
    {
        if (flippedRight)
        {
            rightText.FadeColour(colours.Lime0, 20);
            leftText.FadeColour(disabledColor, 20);
        }
        else
        {
            leftText.FadeColour(colours.Lime0, 20);
            rightText.FadeColour(disabledColor, 20);
        }

        if (inMiddle)
        {
            middleText.FadeColour(colours.Orange1, 40);
        }
        else
        {
            middleText.FadeColour(disabledColor, 40);
        }
        
        prevFlippedRight = flippedRight;
        prevInMiddle = inMiddle;
    }
}
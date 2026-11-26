using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Rulesets.UMania.Edit;
using osuTK;
using osuTK.Graphics;
using Box = osu.Framework.Graphics.Shapes.Box;

namespace UnbeatableStandaloneEditor.Settings;

public partial class QuaverClicker : Container
{
    public QuaverClicker()
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
    }
    private OsuSpriteText countText = null!;
    private ClickableQuaver clickableQuaver = null!;

    private Bindable<int> currentBindable = new Bindable<int>();
    private Bindable<int> patsPowerBindable = new Bindable<int>();

    [Resolved] private OsuConfigManager config { get; set; } = null!;

    [BackgroundDependencyLoader]
    private void load()
    {
        currentBindable = config.GetBindable<int>(OsuSetting.EditorPats);
        patsPowerBindable = config.GetBindable<int>(OsuSetting.EditorPatsPower);

        Children = new Drawable[]
        {

            countText = new OsuSpriteText()
            {
                Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Margin = new MarginPadding() { Left = 80 },
                Alpha = 0,
                Text = currentBindable.Value.ToString()
            },
            clickableQuaver = new ClickableQuaver()
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                X = -10,
                Action = () => quaverClick()
            },
            // upgrades
            new FillFlowContainer()
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                Margin = new MarginPadding() { Right = 130, Top = 20 },
                Spacing = new Vector2(0, 2),
                Children = new Drawable[]
                {
                    new Upgrade("More pats", FontAwesome.Solid.HandPointer, 100, 1.28f, patsPowerBindable, currentBindable),
                    new PlaceholderUpgrade(),
                    new PlaceholderUpgrade()
                }
            }

        };

        currentBindable.BindValueChanged(newValue =>
        {
            updateText(newValue.NewValue);
        });

        if (currentBindable.Value > 0)
            updateText(currentBindable.Value);
    }

    private void quaverClick()
    {
        currentBindable.Value += 1 + patsPowerBindable.Value;
        //updateText(currentBindable.Value);
        clickableQuaver.Squish();
    }

    private void updateText(int newValue)
    {
        countText.Text = newValue.ToString();
        countText.FinishTransforms();
        countText.ClearTransforms();
        countText.Alpha = 1;
        countText.FadeTo(0.65f, 2000, Easing.OutQuint);
    }
}


public partial class ClickableQuaver : OsuClickableContainer
{
    public ClickableQuaver()
    {
        RelativeSizeAxes = Axes.X;
        Height = 1;
    }

    private UbIcon quaverIcon;

    [BackgroundDependencyLoader]
    private void load(OsuConfigManager config)
    {
        OsuSpriteText t;

        Children = new Drawable[]
        {
            quaverIcon = new UbIcon(UbIconType.LegallyDistinctQuaver)
            {
                ForShow = true,
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
            },
        };

        Height = quaverIcon.DrawHeight;
    }

    public void Squish()
    {
        quaverIcon.FinishTransforms();
        quaverIcon.ClearTransforms();
        quaverIcon.Scale = new Vector2(1.25f, 0.75f);
        quaverIcon.ScaleTo(new Vector2(1f, 1f), 700, Easing.OutElastic);
    }
}

public partial class Upgrade : OsuClickableContainer
{
    private string title;
    private IconUsage icon;
    private int basePrice;
    private Bindable<int> currency;
    private Bindable<int> level;
    private float exponent;

    public int UpgradePrice => (int)(basePrice * Math.Pow(exponent, (level.Value)));

    public bool CanBuy => currency.Value >= UpgradePrice;

    public Upgrade(string title, IconUsage icon, int basePrice, float exponent, Bindable<int> level, Bindable<int> currency): base(HoverSampleSet.Button)
    {
        this.title = title;
        this.icon = icon;
        this.basePrice = (int)basePrice;
        this.level = level;
        this.exponent = exponent;
        this.currency = currency;
        Width = 115;
        Height = 36;
        Scale = new Vector2(0.9f, 0.9f);
    }

    private SpriteIcon upgradeIcon;
    private OsuSpriteText priceText;
    private OsuSpriteText levelText;
    private Box backgroundBox;

    [Resolved]
    private OverlayColourProvider colourProvider { get; set; } = null!;


    [BackgroundDependencyLoader]
    private void load()
    {
        OsuSpriteText t;

        Masking = true;
        CornerRadius = 5;

        Child = new Container()
        {
            RelativeSizeAxes = Axes.Both,
            Children = new Drawable[]
            {
                backgroundBox = new Box() { RelativeSizeAxes = Axes.Both, Colour = colourProvider.Background5 },
                new SpriteIcon() {
                    Icon = icon,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding() { Left = 5 },
                    Size = new Vector2(20)
                },
                new OsuSpriteText()
                {
                    Text = title, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft,
                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                    Y = -5,
                    Margin = new MarginPadding() { Left = 30 }
                },
                priceText = new OsuSpriteText()
                {
                    Text = $"Price: {UpgradePrice}", Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft,
                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.Regular),
                    Y = 5,
                    Margin = new MarginPadding() { Left = 30 }
                },
                levelText = new OsuSpriteText()
                {
                    Text = $"lvl. {level.Value}",
                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding() { Right = 5 }
                },
            }
        };

        Action = () =>
        {
            if (CanBuy)
            {
                currency.Value -= UpgradePrice;
                level.Value++;
                UpdateTexts();
            }
            else
            {
                backgroundBox.FinishTransforms();
                backgroundBox.ClearTransforms();
                backgroundBox.Colour = Color4.Red.Darken(0.6f);
                backgroundBox.FadeColour(colourProvider.Background5, 500, Easing.OutQuint);
            }
        };
    }

    protected override bool OnHover(HoverEvent e)
    {
        if (CanBuy)
            backgroundBox.FadeColour(colourProvider.Background5.Lighten(0.15f), 200, Easing.OutQuint);
        return base.OnHover(e);
    }

    protected override void OnHoverLost(HoverLostEvent e)
    {
        backgroundBox.FadeColour(colourProvider.Background5, 200, Easing.OutQuint);
        base.OnHoverLost(e);
    }

    public void UpdateTexts()
    {
        priceText.Text = $"Price: {UpgradePrice}";
        levelText.Text = $"lvl. {level.Value}";
    }
}

public partial class PlaceholderUpgrade : Container
{
    public PlaceholderUpgrade()
    {
        Width = 115;
        Height = 36;
        Scale = new Vector2(0.9f, 0.9f);
    }

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider)
    {
        Masking = true;
        CornerRadius = 5;

        Child = new Container()
        {
            RelativeSizeAxes = Axes.Both,
            Children = new Drawable[]
            {
                new Box() { RelativeSizeAxes = Axes.Both, Colour = colourProvider.Background5 },
                new OsuSpriteText()
                {
                    Text = "???", Anchor = Anchor.Centre, Origin = Anchor.Centre,
                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                },
            }
        };
    }
}

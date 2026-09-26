using System;
using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Custom;


public partial class TooltipNumberInput : Container, IHasTooltip
{
    public static string OffsetTooltip =
        "All notes and timings will have this offset added to them when exporting maps. (Default: 0)\nWhen importing a map that has this offset, you can use the \"Offset all points\" button in the timing tab to move all points back again.\nBe aware that offsets may feel different depending on the song or player.\nOffset may vary between file types. I blame the game engine.";          
    
    private const float height = 24;
    private const float step_width = 18;
    private const float box_width = 56;
    private const float spacing = 3;

    public LocalisableString LabelText { get; set; }

    public LocalisableString TooltipText { get; set; }

    private readonly BindableWithCurrent<int> current = new BindableWithCurrent<int>();
    
    public Bindable<int> Current
    {
        get => current.Current;
        set => current.Current = value;
    }

    public int MinimumValue { get; set; } = int.MinValue;

    public int MaximumValue { get; set; } = int.MaxValue;

    private OsuTextFlowContainer labelText;
    private OsuNumberBox numberBox;

    public TooltipNumberInput()
    {
        AutoSizeAxes = Axes.Y;
        RelativeSizeAxes = Axes.X;
    }

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider)
    {
        Children = new Drawable[]
        {
            labelText = new OsuTextFlowContainer
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Text = LabelText,
            },
            new FillFlowContainer
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(spacing),
                Children = new Drawable[]
                {
                    new NudgeButton
                    {
                        Text = "-",
                        Width = step_width,
                        Height = height,
                        Action = () => nudge(-1),
                        BackgroundColour = colourProvider.Background5,
                    },
                    numberBox = new OsuNumberBox
                    {
                        Width = box_width,
                        Height = height,
                        CommitOnFocusLost = true,
                        SelectAllOnFocus = true,
                        PlaceholderText = "0",
                        FontSize = 15
                    },
                    new NudgeButton
                    {
                        Text = "+",
                        Width = step_width,
                        Height = height,
                        Action = () => nudge(1),
                        BackgroundColour = colourProvider.Background5,
                    },
                },
            },
        };


        numberBox.OnCommit += (_, _) => commit();
        Current.BindValueChanged(e => numberBox.Text = format(e.NewValue), true);
    }

    private void nudge(int amount) => Current.Value = Math.Clamp(Current.Value + amount, MinimumValue, MaximumValue);

    private void commit()
    {
        if (int.TryParse(numberBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            && value >= MinimumValue && value <= MaximumValue)
        {
            Current.Value = value;
            return;
        }

        numberBox.Text = format(Current.Value);
    }

    private static string format(int value) => value.ToString(CultureInfo.InvariantCulture);
}

public partial class NudgeButton : RoundedButton
{
    public NudgeButton()
    {
        HasTriangles = false;
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();
        
        Content.CornerRadius = 6;
    }

    protected override SpriteText CreateText() => new OsuSpriteText
    {
        Depth = -1,
        Origin = Anchor.Centre,
        Anchor = Anchor.Centre,
        Font = OsuFont.GetFont(weight: FontWeight.Regular)
    };
}
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


public partial class TooltipTextInput : Container, IHasTooltip
{
    private const float height = 24;
    private const float step_width = 18;
    private const float box_width = 128;
    private const float spacing = 3;

    public LocalisableString LabelText { get; set; }

    public LocalisableString TooltipText { get; set; }

    private readonly BindableWithCurrent<string> current = new BindableWithCurrent<string>();
    
    public Bindable<string> Current
    {
        get => current.Current;
        set => current.Current = value;
    }

    private OsuTextFlowContainer labelText;
    private OsuTextBox textBox;

    public TooltipTextInput()
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
                    
                    textBox = new OsuTextBox()
                    {
                        Width = box_width,
                        Height = height,
                        CommitOnFocusLost = true,
                        SelectAllOnFocus = true,
                        PlaceholderText = "Name",
                        FontSize = 15
                    },
                },
            },
        };


        textBox.OnCommit += (_, _) => commit();
        Current.BindValueChanged(e => textBox.Text = e.NewValue, true);
    }

    private void commit()
    {
        Current.Value = textBox.Text;
    }

}
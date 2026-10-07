using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Custom;

public partial class EnumDropdownSettingsInput<T> : ContainerWithTooltip where T : struct, Enum
{
    private OsuEnumDropdown<T> enumDropdown;
    
    public string LabelText { get; init; } = "";

    public required Bindable<T> Current { get; init; }
        
    public EnumDropdownSettingsInput() { }

    [BackgroundDependencyLoader]
    private void load()
    {
        AutoSizeAxes = Axes.Y;
        RelativeSizeAxes = Axes.X;
        Children = new Drawable[]
        {
            new OsuTextFlowContainer()
            {
                Text = LabelText,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Y = 2,
            },
            enumDropdown = new OsuEnumDropdown<T>()
            {
                RelativeSizeAxes = Axes.X,
                Width = 0.48f,
                Origin = Anchor.CentreRight,
                Anchor = Anchor.CentreRight,
                Current = { BindTarget = Current },
                Margin = new MarginPadding { Bottom = 4 }
            },
        };
    }
}

public partial class ContainerWithTooltip : Container, IHasTooltip
{
    public LocalisableString TooltipText { get; set; } = "";
}
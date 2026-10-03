using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;

namespace UnbeatableStandaloneEditor.Settings;

public partial class MenuLabel : OsuSpriteText
{
    public MenuLabel(string text)
    {
        Text = text;
        Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold);
        Margin = new MarginPadding { Bottom = 6, Top = 10 };
    }
}

public partial class VolumeSlider : RoundedSliderBar<double>
{
    public VolumeSlider(Bindable<double> volumeBindable)
    {
        RelativeSizeAxes = Axes.X;
        Current = volumeBindable;
        DisplayAsPercentage = true;
    }
}

public partial class TooltipCheckbox : OsuCheckbox, IHasTooltip
{
    public LocalisableString TooltipText { get; set; }
}

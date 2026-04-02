using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osuTK.Graphics;

namespace UnbeatableStandaloneEditor.Settings;

public partial class SettingsPopover : OsuPopover
{
    public SettingsPopover() : base(false)
    { }

    private OsuCheckbox mouseCheckbox;

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider, AudioManager audio, GameHost host, EditorConfigManager editorConfig)
    {
        Child = new FillFlowContainer
        {
            Width = 250,
            AutoSizeAxes = Axes.Y,
            Padding = new MarginPadding(16),
            Direction = FillDirection.Vertical,
            Children = new Drawable[]
            {
                new OsuSpriteText()
                {
                    Text = "Settings",
                    Font = OsuFont.Default.With(size: 18, weight: FontWeight.Bold),
                    Margin = new MarginPadding { Bottom = 10 },
                },
                mouseCheckbox = new OsuCheckbox()
                {
                    LabelText =  "Show system cursor",
                    RelativeSizeAxes = Axes.X,
                    Current = new Bindable<bool>(true),
                    Margin = new MarginPadding { Bottom = 10, Top = 5 },

                },
                new MenuLabel("Master Volume"),
                new VolumeSlider(audio.Volume),
                new MenuLabel("Music"),
                new VolumeSlider(audio.VolumeTrack),
                new MenuLabel("Effects"),
                new VolumeSlider(audio.VolumeSample),

                new OsuSpriteText()
                {
                    AllowMultiline = true,
                    RelativeSizeAxes = Axes.X,
                    Text = "Tip: Use Alt+Shift+Scroll to summon the volume controls anywhere in the editor.",
                    Font = OsuFont.Default.With(size: 12, weight: FontWeight.Regular),
                    Colour = colourProvider.Content1.Opacity(0.75f),
                    Margin = new MarginPadding { Top = 15 },
                }

            }
        };

        mouseCheckbox.Current.Value = editorConfig.Get<bool>(EditorSetting.ShowSystemCursor);
        mouseCheckbox.Current.ValueChanged += e =>
        {
            if (e.NewValue)
                host.Window.CursorState &= ~CursorState.Hidden;
            else
            {
                host.Window.CursorState |= CursorState.Hidden;
            }

            editorConfig.SetValue(EditorSetting.ShowSystemCursor, e.NewValue);
        };

        Add(new Container
        {
            RelativeSizeAxes = Axes.Both,
            Masking = true,
            BorderThickness = 2,
            CornerRadius = 10,
            BorderColour = colourProvider.Highlight1,
            Children = new Drawable[]
            {
                new Box
                {
                    Colour = Color4.Transparent,
                    RelativeSizeAxes = Axes.Both,
                },
            }

        });

    }

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
}

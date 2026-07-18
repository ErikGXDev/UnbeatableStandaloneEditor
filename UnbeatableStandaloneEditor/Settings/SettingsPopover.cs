using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace UnbeatableStandaloneEditor.Settings;

public partial class SettingsPopover : OsuPopover
{
    public SettingsPopover() : base(false)
    { }

    private OsuCheckbox mouseCheckbox = null!;
    private TooltipCheckbox nudgeCheckbox = null!;
    private OsuScrollContainer keybindingsContainer = null!;
    private RoundedButton editKeybindingsButton = null!;

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider, AudioManager audio, GameHost host, EditorConfigManager editorConfig, OsuConfigManager osuConfig)
    {
        Child = new FillFlowContainer
        {
            Width = 340,
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
                new SettingsGroup
                {
                    Label = "General",
                    Controls = new Drawable[]
                    {
                        mouseCheckbox = new OsuCheckbox()
                        {
                            LabelText =  "Show system cursor",
                            RelativeSizeAxes = Axes.X,
                            Current = new Bindable<bool>(true),
                            Margin = new MarginPadding { Bottom = 10, Top = 5 },

                        },
                        nudgeCheckbox = new TooltipCheckbox
                        {
                            LabelText = "Nudge by 1ms (J/K)",
                            TooltipText = "When on, pressing J/K nudges notes 1ms up/down instead of a full beat.\n(This feature may be useful for placement ordering.)",
                            RelativeSizeAxes = Axes.X,
                            Current = osuConfig.GetBindable<bool>(OsuSetting.EditorNudgeByMilliseconds),
                            Margin = new MarginPadding { Bottom = 10 },
                        },
                        new TooltipCheckbox
                        {
                            LabelText = "Play hitsounds in camera lane",
                            TooltipText = "When off, hitting notes in the camera lane will not play their hit sound.",
                            RelativeSizeAxes = Axes.X,
                            Current = osuConfig.GetBindable<bool>(OsuSetting.PlaySamplesInCameraLane),
                            Margin = new MarginPadding { Bottom = 10 },
                        },
                    },
                },
                new SettingsGroup
                {
                    Label = "Volume",
                    Controls = new Drawable[]
                    {
                        new MenuLabel("Master Volume"),
                        new VolumeSlider(audio.Volume),
                        new MenuLabel("Music"),
                        new VolumeSlider(audio.VolumeTrack),
                        new MenuLabel("Effects"),
                        new VolumeSlider(audio.VolumeSample),
                    },
                },

                new SettingsGroup
                {
                    Label = "Websocket",
                    Controls = new Drawable[]
                    {
                        new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Text = "Install the Websocket mod to quickly test your maps in UNBEATABLE through the editor.",
                            Colour = colourProvider.Content1.Opacity(0.75f),
                            Margin = new MarginPadding { Bottom = 8 },
                        },
                        new RoundedButton
                        {
                            Width = 150,
                            Height = 30,
                            Text = "Download",
                            Colour = colourProvider.Colour1,
                            BackgroundColour = colourProvider.Background2,
                            Scale = new Vector2(0.9f),
                            Action = () => BrowserUtil.OpenUrl("https://github.com/ErikGXDev/UnbeatableWebsocket#readme-start"),
                        },
                    },
                },

                new OsuSpriteText
                {
                    Text = "Key Bindings",
                    Font = OsuFont.Default.With(size: 16, weight: FontWeight.Bold),
                    Margin = new MarginPadding { Top = 15, Bottom = 5 },
                },
                keybindingsContainer = new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 190,
                    Masking = true,
                    AlwaysPresent = false,
                    Child = new EditorKeyBindingsSubsection(),
                },
                editKeybindingsButton = new RoundedButton
                {
                    Width = 150,
                    Height = 30,
                    Text = "Click to show...",
                    Colour = colourProvider.Colour1,
                    BackgroundColour = colourProvider.Background2,
                    Scale = new Vector2(0.9f),
                    Action = () =>
                    {
                        keybindingsContainer.Show();
                        editKeybindingsButton.Hide();
                    },
                },

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

        keybindingsContainer.Hide();

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

    public partial class SettingsGroup : FillFlowContainer
    {
        public LocalisableString Label { get; init; }

        public Drawable[]? Controls { get; init; }

        public SettingsGroup()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, 2);
            Margin = new MarginPadding { Bottom = 10 };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Add(new OsuSpriteText
            {
                Text = Label,
                Font = OsuFont.Default.With(size: 17, weight: FontWeight.SemiBold),
                Margin = new MarginPadding { Bottom = 4 },
            });

            if (Controls != null)
                AddRange(Controls);
        }
    }

    private partial class TooltipCheckbox : OsuCheckbox, IHasTooltip
    {
        public LocalisableString TooltipText { get; set; }
    }
}

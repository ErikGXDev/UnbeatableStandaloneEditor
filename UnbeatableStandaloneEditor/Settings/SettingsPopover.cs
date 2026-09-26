using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Custom;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using UnbeatableStandaloneEditor.Components;

namespace UnbeatableStandaloneEditor.Settings;

public partial class SettingsPopover : OsuPopover
{
    public SettingsPopover() : base(false)
    {
    }

    private OsuCheckbox mouseCheckbox = null!;
    private OsuScrollContainer keybindingsContainer = null!;
    private RoundedButton editKeybindingsButton = null!;

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider, AudioManager audio, GameHost host,
        EditorConfigManager editorConfig, OsuConfigManager osuConfig, Storage storage)
    {
        Child = new Container()
        {
            Width = 340,
            Height = 650,
            Child = new OsuScrollContainer()
            {
                Padding = new MarginPadding(6),

                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer()
                {
                    Padding = new MarginPadding(12),
                    Width = 320,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 3),
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
                                    LabelText = "Show system cursor",
                                    RelativeSizeAxes = Axes.X,
                                    Current = new Bindable<bool>(true),
                                    Margin = new MarginPadding { Bottom = 10, Top = 5 },
                                },
                                new TooltipCheckbox
                                {
                                    LabelText = "Nudge by 1ms (J/K)",
                                    TooltipText =
                                        "When on, pressing J/K nudges notes 1ms up/down instead of a full beat.\n(This feature may be useful for placement ordering.)",
                                    RelativeSizeAxes = Axes.X,
                                    Current = osuConfig.GetBindable<bool>(OsuSetting.EditorNudgeByMilliseconds),
                                    Margin = new MarginPadding { Bottom = 10 },
                                },
                                new TooltipCheckbox()
                                {
                                    LabelText = "Reverse scroll in editor",
                                    TooltipText = "Reverses scrolling for the editor timeline.",
                                    RelativeSizeAxes = Axes.X,
                                    Current = osuConfig.GetBindable<bool>(OsuSetting.EditorReverseScroll),
                                    Margin = new MarginPadding { Bottom = 10 },
                                },
                                new TooltipCheckbox
                                {
                                    LabelText = "Play hitsounds in camera lane",
                                    TooltipText =
                                        "When off, hitting notes in the camera lane will not play their hit sound.",
                                    RelativeSizeAxes = Axes.X,
                                    Current = osuConfig.GetBindable<bool>(OsuSetting.PlaySamplesInCameraLane),
                                    Margin = new MarginPadding { Bottom = 10 },
                                },

                                /*new TooltipCheckbox()
                                {
                                    LabelText = "Use old update button",
                                    TooltipText = "Replaces the new automatic updater button with the old \"New version available!\" button, which opens the newest release page in your browser.\nYou can enable this if you prefer to download the editor manually, or if your antivirus is causing issues.",
                                    RelativeSizeAxes = Axes.X,
                                    Current = editorConfig.GetBindable<bool>(EditorSetting.NewDisableUpdater),
                                    Margin = new MarginPadding { Bottom = 10 },
                                },*/
                                /*new TooltipCheckbox
                                {
                                    LabelText = "Swap pink notes in placement order",
                                    TooltipText = "Normally, when you place a camera note and another normal note at the same time, you will have the option to swap them, so either note is in front of the other.\nWhen this setting is enabled, this swap will target pink notes instead of camera notes.",
                                    RelativeSizeAxes = Axes.X,
                                    Current = osuConfig.GetBindable<bool>(OsuSetting.EditorSwapPinkInsteadOfCamera),
                                    Margin = new MarginPadding { Bottom = 10 },
                                },*/
                            },
                        },
                        new SettingsGroup()
                        {
                            Label = "Advanced",
                            Controls = new Drawable[]
                            {
                                new TooltipCheckbox
                                {
                                    LabelText = "4-Key mode in editor",
                                    TooltipText =
                                        "Turns the first two columns into extra top/bottom lanes. Notes in these lanes are flipped automatically when zoomed out.",
                                    RelativeSizeAxes = Axes.X,
                                    Current = osuConfig.GetBindable<bool>(OsuSetting.Editor4KeyMode),
                                    Margin = new MarginPadding { Bottom = 10, Top = 5 },
                                },
                                new TooltipCheckbox
                                {
                                    LabelText = "Key-based charting",
                                    TooltipText =
                                        "Enable keybinds similar to the official editor.\nUse the keys 1-6 to place notes in a lane. Scroll while holding a key to add hold notes. Holding Shift places a Dodge, Double or Zoom instead.",
                                    RelativeSizeAxes = Axes.X,
                                    Current = osuConfig.GetBindable<bool>(OsuSetting.EditorKeyBasedCharting),
                                    Margin = new MarginPadding { Bottom = 10 },
                                },
                                new TooltipCheckbox
                                {
                                    LabelText = "\"Unanimated\" notes",
                                    TooltipText = "Enables new note types for Stefy's downloadable UNANIMATED mod. Adds new \"Animate\" note types for the 2nd lane, which can be edited in a new menu on the right when selected.",
                                    RelativeSizeAxes = Axes.X,
                                    Current = osuConfig.GetBindable<bool>(OsuSetting.EditorUnanimated),
                                    Margin = new MarginPadding { Bottom = 10 },
                                },
                                new TooltipCheckbox
                                {
                                    LabelText = "More hold transparency",
                                    TooltipText = "Hold notes are slightly transparent so you can place notes behind them.\nThis setting makes them even more transparent, in case you have many hold notes at once.",
                                    RelativeSizeAxes = Axes.X,
                                    Current = osuConfig.GetBindable<bool>(OsuSetting.EditorMoreTransparency),
                                    Margin = new MarginPadding { Bottom = 10 },
                                },
                                new TooltipCheckbox
                                {
                                    LabelText = "Long export path fix",
                                    TooltipText = "Enable if UNBEATABLE can't load your exported map because the file name is too long. Makes exported file names shorter.",
                                    RelativeSizeAxes = Axes.X,
                                    Current = osuConfig.GetBindable<bool>(OsuSetting.EditorShortNames),
                                    Margin = new MarginPadding { Bottom = 10 },
                                },
                                new TooltipNumberInput
                                {
                                    LabelText = "Export note offset (ms)",
                                    TooltipText = TooltipNumberInput.OffsetTooltip,
                                    Current = osuConfig.GetBindable<int>(OsuSetting.EditorExportOffsetMs),
                                    MinimumValue = -1000,
                                    MaximumValue = 1000,
                                    Margin = new MarginPadding { Bottom = 10 },
                                },
                            }
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
                        new SettingsGroup()
                        {
                            Label = "Backups",
                            Margin = new MarginPadding() {Top = 10, Bottom = 10},
                            Controls = new Drawable[]
                            {
                                new TooltipCheckbox()
                                {
                                    LabelText = "Automatically create backups",
                                    TooltipText = "Backups are created every time you save or leave the editor.",
                                    RelativeSizeAxes = Axes.X,
                                    Current = osuConfig.GetBindable<bool>(OsuSetting.CreateBackups),
                                    Margin = new MarginPadding { Bottom = 10, Top = 5 },
                                },
                                new OsuTextFlowContainer(t =>
                                    t.Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular))
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Text =
                                        "To use backups, you can import them through the import menu or with drag-and-drop.",
                                    Colour = colourProvider.Content1.Opacity(0.75f),
                                    Margin = new MarginPadding { Bottom = 8 },
                                },
                                new RoundedButton()
                                {
                                    Width = 180,
                                    Height = 30,
                                    Text = "Open backups folder",
                                    Colour = colourProvider.Colour1,
                                    BackgroundColour = colourProvider.Background2,
                                    Scale = new Vector2(0.9f),
                                    Action = () =>
                                        host.OpenFileExternally(storage.GetFullPath("backups")),
                                }
                            },
                        },

                        new SettingsGroup
                        {
                            Label = "Websocket",
                            Controls = new Drawable[]
                            {
                                new OsuTextFlowContainer(t =>
                                    t.Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular))
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Text =
                                        "Install the Websocket mod to quickly test your maps in UNBEATABLE through the editor.",
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
                                    Action = () =>
                                        BrowserUtil.OpenUrl(
                                            "https://github.com/ErikGXDev/UnbeatableWebsocket#readme-start"),
                                },
                            },
                        },

                        new OsuSpriteText
                        {
                            Text = "Key Bindings",
                            Font = OsuFont.Default.With(size: 16, weight: FontWeight.Bold),
                            Margin = new MarginPadding { Top = 10, Bottom = 5 },
                        },
                        keybindingsContainer = new OsuScrollContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 250,
                            Masking = true,
                            Child = new EditorKeyBindingsSubsection(),
                            ScrollDistance = 65
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
            Spacing = new Vector2(0, 1);
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

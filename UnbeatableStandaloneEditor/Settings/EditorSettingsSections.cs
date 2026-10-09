using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Custom;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Screens.Edit;
using osuTK;

namespace UnbeatableStandaloneEditor.Settings;


public abstract partial class EditorSettingsSection : SettingsSection
{
    protected virtual float HorizontalPadding => 12;

    private Box? dimOverlay;

    [BackgroundDependencyLoader]
    private void load()
    {
        var contentContainer = (Container)FlowContent.Parent!;
        var outerContainer = (Container)contentContainer.Parent!;

        contentContainer.Padding = new MarginPadding
        {
            Top = contentContainer.Padding.Top,
            Bottom = contentContainer.Padding.Bottom,
            Left = contentContainer.Padding.Left + HorizontalPadding,
            Right = contentContainer.Padding.Right + HorizontalPadding,
        };

        foreach (var drawable in outerContainer.Children)
        {
            if (drawable is Box box && box.RelativeSizeAxes == Axes.Both)
                dimOverlay = box;
        }
    }

    protected override void Update()
    {
        base.Update();

        // fix odd dimming of unselected options
        if (dimOverlay is { } overlay && overlay.Alpha > 0)
        {
            overlay.ClearTransforms();
            overlay.Alpha = 0;
        }
    }

    protected override bool ShouldBeConsideredForInput(Drawable child) => true;
}

public partial class GeneralSettingsSection : EditorSettingsSection
{
    public override LocalisableString Header => "General";

    public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.Cog };

    private OsuCheckbox mouseCheckbox = null!;

    [BackgroundDependencyLoader]
    private void load(EditorConfigManager editorConfig, OsuConfigManager osuConfig, GameHost host)
    {
        Children = new Drawable[]
        {
            mouseCheckbox = new OsuCheckbox()
            {
                LabelText = "Show system cursor",
                RelativeSizeAxes = Axes.X,
                Current = new Bindable<bool>(true),
                Margin = new MarginPadding { Bottom = 10, Top = 5  },
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
        };

        mouseCheckbox.Current.Value = editorConfig.Get<bool>(EditorSetting.ShowSystemCursor);
        mouseCheckbox.Current.ValueChanged += e =>
        {
            if (e.NewValue)
                host.Window.CursorState &= ~CursorState.Hidden;
            else
                host.Window.CursorState |= CursorState.Hidden;

            editorConfig.SetValue(EditorSetting.ShowSystemCursor, e.NewValue);
        };
    }
}

public partial class AdvancedSettingsSection : EditorSettingsSection
{
    public override LocalisableString Header => "Advanced";

    public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.Wrench };

    [BackgroundDependencyLoader]
    private void load(OsuConfigManager osuConfig)
    {
        var quickExportBindable = new Bindable<QuickExportAction>();

        var waveformBumpBindable = new BindableBool();

        Children = new Drawable[]
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
                TooltipText =
                    "Hold notes are slightly transparent so you can place notes behind them.\nThis setting makes them even more transparent, in case you have many hold notes at once.",
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
            new TooltipCheckbox()
            {
                LabelText = "Use new waveform offset",
                TooltipText = "Increases the waveform visual offset from 20ms to 40ms in order to improve sync with hitsounds. All waveforms will appear slightly earlier, so you may have to adjust your notes as well.",
                RelativeSizeAxes = Axes.X,
                Current = waveformBumpBindable,
                Margin = new MarginPadding { Bottom = 10 },
            },
            new TooltipNumberInput
            {
                LabelText = "Export note offset (ms)",
                TooltipText = TooltipNumberInput.OffsetTooltip,
                Current = osuConfig.GetBindable<int>(OsuSetting.EditorExportOffsetMs),
                MinimumValue = -1000,
                MaximumValue = 1000,
                Margin = new MarginPadding { Bottom = 6 },
            },
            new EnumDropdownSettingsInput<QuickExportAction>()
            {
                LabelText = "Quick-Export button",
                TooltipText = "Choose what the Quick Export button does, located in the bottom-right corner of the editor.",
                Current = quickExportBindable,
                Margin = new MarginPadding { Bottom = 10 },
            }
        };

        quickExportBindable.Value = (QuickExportAction)osuConfig.Get<int>(OsuSetting.EditorQuickExportMode);
        quickExportBindable.BindValueChanged(e =>
        {
            osuConfig.SetValue(OsuSetting.EditorQuickExportMode, (int)e.NewValue);
        });

        waveformBumpBindable.Value = osuConfig.Get<bool>(OsuSetting.Editor40msWaveformBump);
        waveformBumpBindable.BindValueChanged(e =>
        {
            osuConfig.SetValue(OsuSetting.Editor40msWaveformBump, e.NewValue);
            Editor.WAVEFORM_VISUAL_OFFSET = e.NewValue ? 30 : 20;
        });
    }
}

public partial class VolumeSettingsSection : EditorSettingsSection
{
    public override LocalisableString Header => "Volume";

    public override Drawable CreateIcon() => new SpriteIcon { Icon = OsuIcon.Audio };

    [BackgroundDependencyLoader]
    private void load(AudioManager audio, OverlayColourProvider colourProvider)
    {
        Children = new Drawable[]
        {
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
                Margin = new MarginPadding { Top = 8 },
            },
        };
    }
}

public partial class BackupSettingsSection : EditorSettingsSection
{
    public override LocalisableString Header => "Backups";

    public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.Save };

    [BackgroundDependencyLoader]
    private void load(OsuConfigManager osuConfig, OverlayColourProvider colourProvider, GameHost host, Storage storage)
    {
        Children = new Drawable[]
        {
            new TooltipCheckbox()
            {
                LabelText = "Automatically create backups",
                TooltipText = "Backups are created every time you save or leave the editor.",
                RelativeSizeAxes = Axes.X,
                Current = osuConfig.GetBindable<bool>(OsuSetting.CreateBackups),
                Margin = new MarginPadding { Bottom = 10, Top = 5 },
            },
            new TooltipNumberInput
            {
                LabelText = "Backup interval (minutes)",
                TooltipText = "Additionally create a backup automatically this often while you are editing, even if you don't save. Set to 0 to disable.",
                Current = osuConfig.GetBindable<int>(OsuSetting.EditorBackupIntervalMinutes),
                MinimumValue = 0,
                MaximumValue = 240,
                Margin = new MarginPadding { Bottom = 10 },
            },
            new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular))
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text = "To use backups, you can import them through the import menu or with drag-and-drop.",
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
                Action = () => host.OpenFileExternally(storage.GetFullPath("backups")),
            },
        };
    }
}

public partial class WebsocketSettingsSection : EditorSettingsSection
{
    public override LocalisableString Header => "Useful Mods";

    public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.Tools };

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider)
    {
        Children = new Drawable[]
        {
            // WebSocket Mod
            new OsuSpriteText()
            {
                AllowMultiline = true,
                RelativeSizeAxes = Axes.X,
                Text = "Websocket Mod",
                Font = OsuFont.Default.With(size: 18, weight: FontWeight.SemiBold),
                Colour = colourProvider.Content1,
                Margin = new MarginPadding { Top = 5 },
            },
            new OsuSpriteText()
            {
                AllowMultiline = true,
                RelativeSizeAxes = Axes.X,
                Text = "by Erik",
                Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular),
                Colour = colourProvider.Content1,
                Margin = new MarginPadding { Bottom = 8 },
            },
            new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular))
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text = "Install the Websocket mod to quickly test your maps in UNBEATABLE through the editor.\nIf you have the mod installed, simply leave the game open while editing maps to get access to a new Websocket export option.",
                Colour = colourProvider.Content1.Opacity(0.85f),
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

            // Practice Mode
            /*new OsuSpriteText()
            {
                AllowMultiline = true,
                RelativeSizeAxes = Axes.X,
                Text = "Practice Mode",
                Font = OsuFont.Default.With(size: 18, weight: FontWeight.SemiBold),
                Colour = colourProvider.Content1,
                Margin = new MarginPadding { Top = 15 },
            },
            new OsuSpriteText()
            {
                AllowMultiline = true,
                RelativeSizeAxes = Axes.X,
                Text = "by Stefyfresh",
                Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular),
                Colour = colourProvider.Content1,
                Margin = new MarginPadding { Bottom = 5 },
            },
            new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular))
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text = "The practice mode mod allows you to test your maps at different timestamps, in combination with the Websocket mod.",
                Colour = colourProvider.Content1.Opacity(0.85f),
                Margin = new MarginPadding { Bottom = 8, Top = 5 },
            },
            new RoundedButton
            {
                Width = 150,
                Height = 30,
                Text = "Download",
                Colour = colourProvider.Colour1,
                BackgroundColour = colourProvider.Background2,
                Scale = new Vector2(0.9f),
                Action = () => BrowserUtil.OpenUrl("https://github.com/Stefyfresh/UNBEATABLE-practice-mode#unbeatable-practice-mode"),
            },*/
        };
    }
}

public partial class KeyBindingsSettingsSection : EditorSettingsSection
{
    public override LocalisableString Header => "Key Bindings";

    public override Drawable CreateIcon() => new SpriteIcon { Icon = OsuIcon.Input };

    [BackgroundDependencyLoader]
    private void load()
    {
        Children = new Drawable[]
        {
            new EditorKeyBindingsSubsection(),
        };
    }
}

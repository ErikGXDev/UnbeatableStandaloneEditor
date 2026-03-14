using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Configuration;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osu.Game.Screens;
using UnbeatableStandaloneEditor.BeatmapPicker;

namespace UnbeatableStandaloneEditor;

public partial class MainGame : OsuGameBase, IKeyBindingHandler<GlobalAction>
{
    [Cached(typeof(IDialogOverlay))]
    private readonly DialogOverlay dialogOverlay = new DialogOverlay();

    [Cached]
    private VolumeOverlay volumeOverlay = new VolumeOverlay();

    private OsuScreenStack screenStack = null!;
    private EditorConfigManager editorConfig = null!;

    [Cached]
    private OnScreenDisplay onScreenDisplay = new OnScreenDisplay();

    [BackgroundDependencyLoader]
    private void load(Storage storage)
    {
        editorConfig = new EditorConfigManager(storage);

        Add(screenStack = new OsuScreenStack { RelativeSizeAxes = Axes.Both });

        Add(dialogOverlay);
        Add(volumeOverlay);
        Add(onScreenDisplay);
    }

    public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
    {
        if (e.Repeat) return false;

        // Exit on Esc
        if (e.Action == GlobalAction.Back && screenStack.CurrentScreen is OsuScreen current && current.AllowUserExit)
        {
            screenStack.Exit();
            return true;
        }


        return false;
    }

    public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e) { }

    // Volume controls
    protected override bool OnScroll(ScrollEvent e)
    {
        if (e.AltPressed && e.ShiftPressed)
        {
            float delta = e.ScrollDelta.Y != 0 ? e.ScrollDelta.Y : e.ScrollDelta.X;

            Audio.Volume.Value += delta * 0.05f;
            Logger.Log("Volume adjusted via scroll wheel: " + Audio.Volume.Value+ " - " +e.ScrollDelta.Y * 0.05);
            volumeOverlay?.Show();
            return true;
        }

        return base.OnScroll(e);
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        // Set default volume levels on first launch
        if (editorConfig.Get<bool>(EditorSetting.FirstLaunch))
        {
            Audio.Volume.Value = 0.5;
            Audio.VolumeSample.Value = 0.3;
            Audio.VolumeTrack.Value = 0.7;
            editorConfig.SetValue(EditorSetting.FirstLaunch, false);
            editorConfig.Save();

            LocalConfig.SetValue(OsuSetting.EditorAutoSeekOnPlacement, false);
            LocalConfig.SetValue(OsuSetting.EditorShowSpeedChanges, true);
        }

        var maniaRuleset = UbRuleset.GetRulesetInfo();
        Ruleset.Value = maniaRuleset;

        // DummyWorkingBeatmap triggers the editor to auto-create a new blank beatmap.
        // To open an existing one: Beatmap.Value = BeatmapManager.GetWorkingBeatmap(info);
         // leaves it as DummyWorkingBeatmap
        Beatmap.SetDefault();

        //screenStack.Push(new DirectEditorLoader());
        screenStack.Push(new BeatmapPickerScreen());
    }
}

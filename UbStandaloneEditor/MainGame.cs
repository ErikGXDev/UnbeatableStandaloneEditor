using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Game;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osu.Game.Screens;

namespace UbStandaloneEditor;

public partial class MainGame : OsuGameBase, IKeyBindingHandler<GlobalAction>
{
    [Cached(typeof(IDialogOverlay))]
    private readonly DialogOverlay dialogOverlay = new DialogOverlay();

    [Cached(typeof(INotificationOverlay))]
    private readonly NotificationOverlay notificationOverlay = new NotificationOverlay();

    [Cached]
    private VolumeOverlay volumeOverlay = new VolumeOverlay();
    
    private OsuScreenStack screenStack = null!;

    [BackgroundDependencyLoader]
    private void load()
    {

        Add(dialogOverlay);
        Add(notificationOverlay);

        Add(screenStack = new OsuScreenStack { RelativeSizeAxes = Axes.Both });
        
        Add(volumeOverlay);
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
        if (e.ControlPressed && e.ShiftPressed)
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

        var maniaRuleset = RulesetStore.GetRuleset("umania")!;
        Ruleset.Value = maniaRuleset;

        // DummyWorkingBeatmap triggers the editor to auto-create a new blank beatmap.
        // To open an existing one: Beatmap.Value = BeatmapManager.GetWorkingBeatmap(info);
         // leaves it as DummyWorkingBeatmap
        Beatmap.SetDefault();
         

        //screenStack.Push(new DirectEditorLoader());
        screenStack.Push(new BeatmapPickerScreen());
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using osu.Framework.Allocation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Rulesets.UMania.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit;

// Base code from Mania, is inherited by UnbeatableHitObjectComposer which adds more functionality
[Cached]
public abstract partial class ManiaHitObjectComposer : ScrollingHitObjectComposer<ManiaHitObject>
{
    private DrawableManiaEditorRuleset drawableRuleset = null!;

    [Resolved]
    private EditorScreenWithTimeline? screenWithTimeline { get; set; }

    protected ManiaHitObjectComposer(Ruleset ruleset)
        : base(ruleset)
    {
    }

    public new ManiaPlayfield Playfield => drawableRuleset.Playfield;

    public IScrollingInfo ScrollingInfo => drawableRuleset.ScrollingInfo;

    protected override Playfield PlayfieldAtScreenSpacePosition(Vector2 screenSpacePosition) =>
        Playfield.GetColumnByPosition(screenSpacePosition);

    protected override DrawableRuleset<ManiaHitObject> CreateDrawableRuleset(Ruleset ruleset, IBeatmap beatmap,
                                                                             IReadOnlyList<Mod> mods) =>
        drawableRuleset = new DrawableManiaEditorRuleset(ruleset, beatmap, mods);

    protected override ComposeBlueprintContainer CreateBlueprintContainer()
        => new ManiaBlueprintContainer(this);

    protected override BeatSnapGrid CreateBeatSnapGrid() => new ManiaBeatSnapGrid();

    public override string ConvertSelectionToString()
        => string.Join(',',
            EditorBeatmap.SelectedHitObjects.Cast<ManiaHitObject>().OrderBy(h => h.StartTime)
                         .Select(h => $"{h.StartTime}|{h.Column}"));

    // 123|0,456|1,789|2 ...
    private static readonly Regex selection_regex = new Regex(@"^\d+\|\d+(,\d+\|\d+)*$", RegexOptions.Compiled);

    public override void SelectFromTimestamp(double timestamp, string objectDescription)
    {
        if (!selection_regex.IsMatch(objectDescription))
            return;

        List<ManiaHitObject> remainingHitObjects = EditorBeatmap.HitObjects.Cast<ManiaHitObject>()
                                                                .Where(h => h.StartTime >= timestamp).ToList();
        string[] objectDescriptions = objectDescription.Split(',');

        for (int i = 0; i < objectDescriptions.Length; i++)
        {
            string[] split = objectDescriptions[i].Split('|');
            if (split.Length != 2)
                continue;

            if (!double.TryParse(split[0], out double time) || !int.TryParse(split[1], out int column))
                continue;

            ManiaHitObject? current =
                remainingHitObjects.FirstOrDefault(h => h.StartTime == time && h.Column == column);

            if (current == null)
                continue;

            EditorBeatmap.SelectedHitObjects.Add(current);

            if (i < objectDescriptions.Length - 1)
                remainingHitObjects = remainingHitObjects
                                      .Where(h => h != current && h.StartTime >= current.StartTime).ToList();
        }
    }

    protected override void Update()
    {
        base.Update();

        if (screenWithTimeline?.TimelineArea.Timeline != null)
            drawableRuleset.TimelineTimeRange = EditorClock.TrackLength /
                                                screenWithTimeline.TimelineArea.Timeline.CurrentZoom.Value / 2;
    }
}

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit;

public partial class ManiaBlueprintContainer : ComposeBlueprintContainer
{
    public new ManiaHitObjectComposer Composer => (ManiaHitObjectComposer)base.Composer;

    public ManiaBlueprintContainer(ManiaHitObjectComposer composer)
        : base(composer)
    {
    }

    public override HitObjectSelectionBlueprint? CreateHitObjectBlueprintFor(HitObject hitObject)
    {
        switch (hitObject)
        {
            case Note note:
                return new NoteSelectionBlueprint(note);

            case HoldNote holdNote:
                return new HoldNoteSelectionBlueprint(holdNote);
        }

        return base.CreateHitObjectBlueprintFor(hitObject);
    }

    protected override SelectionHandler<HitObject> CreateSelectionHandler() => new ManiaSelectionHandler();

    protected sealed override DragBox CreateDragBox() => new ScrollingDragBox(Composer.Playfield);

    protected override bool TryMoveBlueprints(DragEvent e, IList<(SelectionBlueprint<HitObject> blueprint, Vector2[] originalSnapPositions)> blueprints)
    {
        Vector2 distanceTravelled = e.ScreenSpaceMousePosition - e.ScreenSpaceMouseDownPosition;

        // The final movement position, relative to movementBlueprintOriginalPosition.
        Vector2 movePosition = blueprints.First().originalSnapPositions.First() + distanceTravelled;

        // Retrieve a snapped position.
        var result = Composer.FindSnappedPositionAndTime(movePosition);

        var referenceBlueprint = blueprints.First().blueprint;
        bool moved = SelectionHandler.HandleMovement(new MoveSelectionEvent<HitObject>(referenceBlueprint, result.ScreenSpacePosition - referenceBlueprint.ScreenSpaceSelectionPoint));
        if (moved)
            ApplySnapResultTime(result, referenceBlueprint.Item.StartTime);
        return moved;
    }
}

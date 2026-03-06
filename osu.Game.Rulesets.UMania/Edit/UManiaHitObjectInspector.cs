using System.Collections.Generic;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Rulesets.UMania.Edit;

public partial class UManiaHitObjectInspector : HitObjectInspector
{
    protected override void AddInspectorValues(HitObject[] objects)
    {
        base.AddInspectorValues(objects);

        if (objects.Length == 1)
        {
            var maniaObject = (ManiaHitObject)objects[0];

            findFlipAndZoom(maniaObject.StartTime, out bool flippedRight, out bool zoomedIn);


            if (maniaObject.Column == 4)
            {
                if (isZoomNote(maniaObject))
                {
                    AddHeader("Action");
                    AddValue((zoomedIn ? "Zoom In" : "Zoom Out"));
                }
                else
                {
                    AddHeader("Action");
                    AddValue((flippedRight ? "Flip to Right" : "Flip to Left"));
                }


            }
            else
            {
                if (maniaObject.Column is 2 or 3 or 5)
                {
                    AddHeader("Row");

                    AddValue(maniaObject.Column == 2 ? "Top" : maniaObject.Column == 3 ? "Bottom" : "Middle");
                }


                AddHeader("Side");
                AddValue((flippedRight ? "Right" : "Left") + " Side");

                AddHeader("Zoom");
                AddValue("Zoomed " + (zoomedIn ? "In" : "Out"));
            }
        }
    }

    private void findFlipAndZoom(double time, out bool flippedRight, out bool zoomedIn)
    {
        flippedRight = true;
        zoomedIn = true;

        // for every flip note, invert the direction

        var notes = (List<ManiaHitObject>)EditorBeatmap.HitObjects;

        foreach (var note in notes)
        {
            if (note.StartTime > time) break;

            if (note.Column == 4)
            {
                if (isZoomNote(note))
                {
                    zoomedIn = !zoomedIn;
                }
                else
                {
                    flippedRight = !flippedRight;
                }
            }
        }
    }

    private bool isZoomNote(HitObject hitObject) => hitObject.Samples.Any(info => info.Name == HitSampleInfo.HIT_WHISTLE);
}


// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;

namespace osu.Game.Rulesets.UMania.Edit.Checks
{
    public class CheckManiaConcurrentObjects : CheckConcurrentObjects
    {
        public override IEnumerable<Issue> Run(BeatmapVerifierContext context)
        {
            var hitObjects = context.CurrentDifficulty.Playable.HitObjects;

            UbNoteBuilder ubHelper1 = new UbNoteBuilder(null);
            UbNoteBuilder ubHelper2 = new UbNoteBuilder(null);

            for (int i = 0; i < hitObjects.Count - 1; ++i)
            {
                var hitobject = hitObjects[i];
                
                ubHelper1.ChangeHitObject(hitobject);

                for (int j = i + 1; j < hitObjects.Count; ++j)
                {
                    var nextHitobject = hitObjects[j];
                    
                    ubHelper2.ChangeHitObject(nextHitobject);

                    // Column 4 hitobjects can be concurrent, so skip them
                    if ((hitobject as IHasColumn)?.Column == 4 || (nextHitobject as IHasColumn)?.Column == 4)
                        continue;
                    
                    var ubIconType1 = ubHelper1.InferObjectTypeIcon();
                    var ubIconType2 = ubHelper2.InferObjectTypeIcon();
                    
                    var column1 = (hitobject as IHasColumn)?.Column;
                    var column2 = (nextHitobject as IHasColumn)?.Column;
                    
                    // Hold notes can be concurrent if the first note is a Double note and the other note
                    // starts at the end time of the Double note.
                    if (ubIconType1 == UbIconType.Double)
                    {
                        // Only check if end of double note collides with the start of the next note
                        if (Math.Abs(hitobject.GetEndTime() - nextHitobject.StartTime) < 10)
                        {
                            // If the double starts at column 2, and the next note starts at column 3,
                            // there would be a collision since double notes always end on an opposite column
                            if ((column1 == 2 && column2 == 3) || (column1 == 3 && column2 == 2))
                            {
                                yield return new IssueTemplateConcurrent(this).Create(hitobject, nextHitobject);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        
                    }
                    
                    // Mania hitobjects are only considered concurrent if they also share the same column.
                    if (column1 != column2)
                        continue;
                    

                    // Two hitobjects cannot be concurrent without also being concurrent with all objects in between.
                    // So if the next object is not concurrent or almost concurrent, then we know no future objects will be either.
                    if (!AreConcurrent(hitobject, nextHitobject) && !AreAlmostConcurrent(hitobject, nextHitobject))
                        break;

                    if (AreConcurrent(hitobject, nextHitobject))
                    {
                        yield return new IssueTemplateConcurrent(this).Create(hitobject, nextHitobject);
                    }
                    /*else if (AreAlmostConcurrent(hitobject, nextHitobject))
                    {
                        //yield return new IssueTemplateAlmostConcurrent(this).Create(hitobject, nextHitobject);
                    }*/
                }
            }
        }
    }
}

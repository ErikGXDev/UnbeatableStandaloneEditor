using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Rulesets.UMania.UI;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit
{
    // Overlay for showing the swap order buttons
    public partial class UbNoteOrderButtonLayer : CompositeDrawable
    {
        private readonly Stage stage;
        private bool refreshScheduled;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved] private UnbeatableHitObjectComposer composer { get; set; } = null!;

        // Column 2 = Top
        // Column 3 = Bottom
        // Column 5 = Middle
        // (Count from 0)
        
        // Each entry tracks a same-time pair and its button
        private readonly List<(ManiaHitObject col2Note, ManiaHitObject col3Note, OrderToggleButton button)> pairs = new();
        
        // Separate list for situations including all 3 columns
        private readonly List<(ManiaHitObject col2Note, ManiaHitObject col3Note, ManiaHitObject col5Note, OrderToggleButton button)> triplePairs = new();
        // (For 3 column situations it should basically always be so col5 is either in front of after both, col2 and col3 are basically one single entity here.)
        // So a pair here can occur when a col5 and col3 note, a col5 and col2 note, or a col5, col3 and col2 note share the same time.
        
        
        public UbNoteOrderButtonLayer(Stage stage)
        {
            this.stage = stage;
            RelativeSizeAxes = Axes.Both;
        }

        protected override void LoadComplete()
        {

            composer.SettingShowPlacementOrder.ValueChanged += (ev) =>
            {
                if (ev.NewValue == TernaryState.True)
                {
                    scheduleRefresh();
                    Alpha = 1;
                }
                else
                {
                    Alpha = 0;
                }
            };
            
            
            
            base.LoadComplete();

            // Handles big changes better than single HitObjectAdded/Removed events
            editorBeatmap.BeatmapReprocessed += onBeatmapReprocessed;

            scheduleRefresh();
            
            Alpha = composer.SettingShowPlacementOrder.Value == TernaryState.True ? 1 : 0;
            
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            editorBeatmap.BeatmapReprocessed -= onBeatmapReprocessed;
        }

        // Fix the buttons not firing normally
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            if (base.ReceivePositionalInputAt(screenSpacePos)) return true;

            foreach (var (_, _, button) in pairs)
            {
                if (button.Alpha > 0 && button.ReceivePositionalInputAt(screenSpacePos))
                    return true;
            }

            return false;
        }

        private void onBeatmapReprocessed() => scheduleRefresh();

        private void scheduleRefresh()
        {
            if (refreshScheduled)
                return;

            refreshScheduled = true;
            Schedule(() =>
            {
                refreshScheduled = false;
                refreshPairs();
            });
        }

        private void refreshPairs()
        {
            ClearInternal();
            pairs.Clear();
            triplePairs.Clear();

            if (stage.Columns.Length < 4) return;

            // For pairs
            // Find all time positions where both column 2 and column 3 have at least one note
            var relevantObjects = editorBeatmap.HitObjects
                .OfType<ManiaHitObject>()
                .Where(h => h.Column == 2 || h.Column == 3)
                .GroupBy(h => h.StartTime);

            foreach (var group in relevantObjects)
            {
                var col2Note = group.FirstOrDefault(h => h.Column == 2);
                var col3Note = group.FirstOrDefault(h => h.Column == 3);

                if (col2Note == null || col3Note == null) continue;
                
                var ubHelper2 = new UbNoteBuilder(col2Note);
                var type2 = ubHelper2.InferObjectTypeIcon();
                
                var ubHelper3 = new UbNoteBuilder(col3Note);
                var type3 = ubHelper3.InferObjectTypeIcon();

                var invalidPairTypes = new[]
                {
                    UbIconType.Note,
                    UbIconType.Hold,
                };
                
                var isInvalidPair = false;
                

                foreach (var type in invalidPairTypes)
                {
                    if (type2 == type && type3 == type)
                    {
                        isInvalidPair = true;
                        break;
                    }
                }
                
                if (isInvalidPair) continue;
                
                
                // True = column 2 (top) appears first
                bool isCol2First = editorBeatmap.FindIndex(col2Note) < editorBeatmap.FindIndex(col3Note);

                var button = new OrderToggleButton
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    IsTopFirst = isCol2First,
                    MiddleMode = false,
                    MiddleMixed = false,
                    Alpha = 0, // hidden until Update() positions it
                };

                var capturedCol2 = col2Note;
                var capturedCol3 = col3Note;
                button.OnToggle = () => swapOrder(capturedCol2, capturedCol3);

                AddInternal(button);
                pairs.Add((col2Note, col3Note, button));
            }
            
            // For 3 column pairs
            var relevantTripleObjects = editorBeatmap.HitObjects
                .OfType<ManiaHitObject>()
                .Where(h => h.Column == 2 || h.Column == 3 || h.Column == 5)
                .GroupBy(h => h.StartTime);

            foreach (var group in relevantTripleObjects)
            {
                var col2Note = group.FirstOrDefault(h => h.Column == 2);
                var col3Note = group.FirstOrDefault(h => h.Column == 3);
                var col5Note = group.FirstOrDefault(h => h.Column == 5);

                if (col5Note == null || (col2Note == null && col3Note == null)) continue;

                bool hasCol2 = col2Note != null;
                bool hasCol3 = col3Note != null;

                
                
                // check if the col5 note is inbetween the col2 and col3 note
                int col5Index = editorBeatmap.FindIndex(col5Note);
                int col2Index = hasCol2 ? editorBeatmap.FindIndex(col2Note!) : -1;
                int col3Index = hasCol3 ? editorBeatmap.FindIndex(col3Note!) : -1;

                
                bool isMixed =
                    hasCol2 && hasCol3 &&
                    ((col5Index > col2Index && col5Index < col3Index) ||
                     (col5Index > col3Index && col5Index < col2Index));
                
                // check if the col5 note is in front of both 
                bool isCol5First =
                    (!hasCol2 || col5Index < col2Index) &&
                    (!hasCol3 || col5Index < col3Index);
                


                var button = new OrderToggleButton
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    IsTopFirst = isCol5First, // Top refers to Col5 here
                    MiddleMode = true,
                    MiddleMixed = isMixed,
                    Alpha = 0, // hidden until Update() positions it
                };

                // When swapping, we should ensure that the col5 note will definitely end up
                // either in front or after both the col2 and col3 notes.
                // The col5 note should never be inbetween the two.
                button.OnToggle = () =>
                {
                    if (hasCol2 && hasCol3)
                    {
                        // Both col2 and col3 exist: toggle col5 between front and back of both
                        if (button.IsTopFirst)
                        {
                            // Col5 is in front, move it to the back
                            editorBeatmap.Remove(col5Note);
                            Schedule(() => editorBeatmap.Add(col5Note));
                        }
                        else
                        {
                            // Col5 is in back (or mixed), move it to the front
                            editorBeatmap.Remove(col5Note);
                            Schedule(() => editorBeatmap.Insert(0, col5Note));
                        }
                    }
                    else
                    {
                        // Only one of col2/col3 exists: just swap col5 with that note
                        var otherNote = col2Note ?? col3Note!;
                        swapOrder(col5Note, otherNote);
                    }
                };
                
                AddInternal(button);
                triplePairs.Add((col2Note, col3Note, col5Note, button)!);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (stage.Columns.Length < 4) return;

            var refColumn = stage.Columns[2];

            // Stage left edge in this layer's local coordinate space
            float stageLeftX = ToLocalSpace(stage.ScreenSpaceDrawQuad.TopLeft).X;
            
            float stageRightX = ToLocalSpace(stage.ScreenSpaceDrawQuad.TopRight).X;

            // Pairs go on the left
            foreach (var (col2Note, _, button) in pairs)
            {
                // Use the columns scrolling container to get screen-space Y at the note's time
                Vector2 screenPos = refColumn.HitObjectContainer.ScreenSpacePositionAtTime(col2Note.StartTime);
                float y = ToLocalSpace(screenPos).Y;

                
                button.X = stageLeftX - 4 - button.DrawWidth;
                button.Y = y - button.DrawHeight / 1.3f;

                // Only show the button when the note is within the visible scrolling area
                button.Alpha = y >= 0 && y <= DrawHeight ? 1f : 0f;
            }
            
            // Tripe pairs go to the right
            foreach (var (col2Note, col3Note, col5Note, button) in triplePairs)
            {
                // Use whichever column note exists (prefer col2 if both exist)
                var refNote = col2Note ?? col3Note!;
                Vector2 screenPos = refColumn.HitObjectContainer.ScreenSpacePositionAtTime(refNote.StartTime);
                float y = ToLocalSpace(screenPos).Y;

                button.X = stageRightX + 4;
                button.Y = y - button.DrawHeight / 1.3f;

                // Only show the button when the note is within the visible scrolling area
                button.Alpha = y >= 0 && y <= DrawHeight ? 1f : 0f;
            }
        }

        private void swapOrder(ManiaHitObject note1, ManiaHitObject note2)
        {
            int idx1 = editorBeatmap.FindIndex(note1);
            int idx2 = editorBeatmap.FindIndex(note2);

            if (idx1 < 0 || idx2 < 0 || idx1 == idx2) return;

            // The note that currently appears first will be removed and re-added,
            // which places it after the other note
            ManiaHitObject firstNote = idx1 < idx2 ? note1 : note2;

            // Remove now, but schedule the re-add to the next frame
            // If both operations happen synchronously, something might happen
            editorBeatmap.Remove(firstNote);
            Schedule(() => editorBeatmap.Add(firstNote));
        }
    }
}

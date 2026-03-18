using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
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

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        // Each entry tracks a same-time pair and its button
        private readonly List<(ManiaHitObject col2Note, ManiaHitObject col3Note, OrderToggleButton button)> pairs = new();

        public UbNoteOrderButtonLayer(Stage stage)
        {
            this.stage = stage;
            RelativeSizeAxes = Axes.Both;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            editorBeatmap.HitObjectAdded += onHitObjectChanged;
            editorBeatmap.HitObjectRemoved += onHitObjectChanged;

            refreshPairs();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            editorBeatmap.HitObjectAdded -= onHitObjectChanged;
            editorBeatmap.HitObjectRemoved -= onHitObjectChanged;
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

        private void onHitObjectChanged(HitObject _) => refreshPairs();

        private void refreshPairs()
        {
            ClearInternal();
            pairs.Clear();

            if (stage.Columns.Length < 4) return;

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
                    Alpha = 0, // hidden until Update() positions it
                };

                var capturedCol2 = col2Note;
                var capturedCol3 = col3Note;
                button.OnToggle = () => swapOrder(capturedCol2, capturedCol3);

                AddInternal(button);
                pairs.Add((col2Note, col3Note, button));
            }
        }

        protected override void Update()
        {
            base.Update();

            if (stage.Columns.Length < 4) return;

            var refColumn = stage.Columns[2];

            // Stage left edge in this layer's local coordinate space
            float stageLeftX = ToLocalSpace(stage.ScreenSpaceDrawQuad.TopLeft).X;

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

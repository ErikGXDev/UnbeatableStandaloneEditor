// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.UMania.Edit.Blueprints
{
    public partial class NotePlacementBlueprint : ManiaPlacementBlueprint<Note>
    {
        private Circle piece = null!;
        private static bool paintDragActive;
        private static (int Column, double Time)? lastPaintPlacement;

        public NotePlacementBlueprint()
            : base(new Note())
        {
        }
        
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;
        
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            RelativeSizeAxes = Axes.Both;
            Masking = true;

            InternalChild = piece = new Circle
            {
                Origin = Anchor.Centre,
                Colour = colours.Yellow,
                Height = 10
            };
        }

        public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double referenceTime)
        {
            var result = base.UpdateTimeAndPosition(screenSpacePosition, referenceTime);

            if (result.Playfield != null)
            {
                piece.Width = result.Playfield.DrawWidth;
                piece.Position = ToLocalSpace(result.ScreenSpacePosition);
            }

            tryPlaceWhileDragging();

            return result;
        }

        private InputManager? inputManager;

        private void tryPlaceWhileDragging()
        {
            if (!paintDragActive)
                return;

            if (inputManager == null)
                inputManager = GetContainingInputManager();

            if (inputManager == null || !inputManager.CurrentState.Mouse.IsPressed(MouseButton.Left))
            {
                paintDragActive = false;
                return;
            }

            if (PlacementActive != PlacementState.Waiting || Column == null)
                return;

            var currentPlacement = (Column.Index, Math.Round(HitObject.StartTime));

            // keep this if looping editorBeatmap becomes too costly
            /*if (lastPaintPlacement == currentPlacement)
                return;*/

            if (noteExistsAtTime(Column.Index, HitObject.StartTime))
                return;

            lastPaintPlacement = currentPlacement;
            EndPlacement(true);
        }
        
        // absolutely not performant but I also trust the CPUs of the modern times
        private bool noteExistsAtTime(int columnIndex, double time)
        {
            foreach (var note in editorBeatmap.HitObjects)
            {
                if (note is Note n && n.Column == columnIndex && (int)Math.Round(n.StartTime) == (int)Math.Round(time))
                    return true;
            }

            return false;
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left)
                return false;

            base.OnMouseDown(e);

            if (config.Get<bool>(OsuSetting.EditorPaintMode))
                paintDragActive = true;
            lastPaintPlacement = Column != null ? (Column.Index, Math.Round(HitObject.StartTime)) : null;

            // Place the note immediately.
            EndPlacement(true);

            return true;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButton.Left)
                paintDragActive = false;
        }
    }
}

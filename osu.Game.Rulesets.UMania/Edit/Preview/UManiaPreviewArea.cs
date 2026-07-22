using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.UMania.Edit
{
    public partial class UManiaPreviewArea : CompositeDrawable
    {
        private const float preview_width = 310;
        private const float preview_height = 120;

        private const float inspector_padding = 12;

        private const float left_receptor = 0.40f;
        private const float right_receptor = 0.60f;

        private const float top_receptor = 0.25f;
        private const float bottom_receptor = 0.75f;
        private const float middle_receptor = 0.5f;

        private const float cam_left_receptor = 0.25f;
        private const float cam_right_receptor = 0.75f;
        private const float cam_middle_receptor = 0.5f;

        private const double view_field = 800;
        private const double view_field_tolerance = 400;


        private ExpandingToolboxContainer rightToolbox = null!;

        private Container notesLayer = null!;
        private Container decoLayer = null!;
        private PreviewIndicator indicatorLayer = null!;
        private Box cameraBorder;

        private List<Circle> hitCircles = new List<Circle>();

        private List<PreviewNote> notePool = new List<PreviewNote>();
        private List<PreviewHold> holdPool = new List<PreviewHold>();
        private int activeNoteCount;
        private int activeHoldCount;

        private const double flash_duration = 150;
        private const double hit_window = 50;


        [Resolved] private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved] private EditorClock clock { get; set; } = null!;

        [Resolved] private OsuColour colours { get; set; } = null!;
        [Resolved] private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved] private UnbeatableHitObjectComposer composer { get; set; } = null!;


        public ExpandingToolboxContainer RightToolbox
        {
            set => rightToolbox = value;
        }

        public UManiaPreviewArea()
        {
            Anchor = Anchor.BottomRight;
            Origin = Anchor.BottomRight;
            Width = preview_width;
            Height = preview_height;
            Depth = 1;
        }

        [BackgroundDependencyLoader]
        private void load(EditorBeatmap beatmap)
        {
            InternalChildren = new[]
            {
                indicatorLayer = new PreviewIndicator
                {
                    RelativePositionAxes = Axes.X,
                    Position = new Vector2(0.5f, -8),
                    Depth = 2
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    CornerRadius = 4,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Name = "Preview Border",
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background3,
                        },
                        new Container
                        {
                            Name = "Preview Background",
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding(4),
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = colourProvider.Background2,
                                },
                                cameraBorder = new Box
                                {
                                    Origin = Anchor.Centre,
                                    RelativePositionAxes = Axes.Both,
                                    Colour = colourProvider.Background1.Lighten(0.2f).Opacity(0.4f),
                                    Depth = -1,
                                },
                                decoLayer = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                },

                                notesLayer = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                },
                            }
                        }
                    }
                }
            };

            if (rightToolbox != null)
                rightToolbox.Expanded.BindValueChanged(_ => updateToolboxOffset(), true);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            composer.SettingShowPreview.BindValueChanged(
                _ => this.FadeTo(composer.SettingShowPreview.Value == TernaryState.True ? 1 : 0, 200, Easing.OutQuint),
                true);

            List<float[]> pairs =
            [
                [left_receptor, top_receptor], [left_receptor, bottom_receptor], [right_receptor, top_receptor],
                [right_receptor, bottom_receptor]
            ];

            List<(float, Colour4)> sizePairs =
                [(32, colourProvider.Background3.Darken(0.1f)), (26, colourProvider.Background2)];

            foreach (float[] pair in pairs)
            {
                var receptorX = pair[0];
                var receptorY = pair[1];

                foreach (var pair2 in sizePairs)
                {
                    var size = pair2.Item1;
                    var colour = pair2.Item2;

                    var hitCircle = new Circle()
                    {
                        Colour = colour,
                        Masking = true,
                        RelativePositionAxes = Axes.Both,
                        Position = new Vector2(receptorX, receptorY),
                        Size = new Vector2(size),
                        Origin = Anchor.Centre,
                    };


                    decoLayer.Add(hitCircle);

                    hitCircles.Add(hitCircle);
                }
            }

            ;

            List<float> lines = [left_receptor, right_receptor];

            foreach (float receptor in lines)
            {
                decoLayer.Add(new Box()
                {
                    Colour = colourProvider.Background3.Darken(0.1f),
                    RelativePositionAxes = Axes.Both,
                    RelativeSizeAxes = Axes.Y,
                    Width = 3.4f,
                    Position = new Vector2(receptor, 0),
                    Origin = Anchor.TopCentre,
                });
            }
        }

        private void updateToolboxOffset()
        {
            if (rightToolbox == null)
                return;

            float offset = rightToolbox.Expanded.Value ? rightToolbox.Width - 50 + inspector_padding : 0;
            this.MoveToX(-offset, 200, Easing.OutQuint);
        }

        private int getDoubleEndLane(int startLane)
        {
            if (startLane == 2) return 3;
            if (startLane == 3) return 2;
            
            if (composer.Is4Key)
            {
                if (startLane == 0) return 1;
                if (startLane == 1) return 0;
            }
            return startLane;
        }


        protected override void Update()
        {
            base.Update();

            if (Alpha == 0) return; // Don't update if preview is hidden

            foreach (var note in notePool)
                note.Hide();
            foreach (var hold in holdPool)
                hold.Hide();
            activeNoteCount = 0;
            activeHoldCount = 0;

            double time = clock.CurrentTime;
            bool centerForUpcomingFlip = shouldCenterForUpcomingFlip(time);

            bool flippedRight = true;
            bool zoomedIn = true;
            bool olFlippedRight = true;
            bool olZoomedIn = true;

            bool camUpdated = false;


            foreach (var obj in editorBeatmap.HitObjects)
            {
                if (obj is not ManiaHitObject note)
                    continue;
                
                int column = note.Column;
                
                if (!composer.Is4Key && (column == 0 || column == 1))
                    continue;
                

                // Target camera notes
                if (column == 4)
                {
                    if (note.StartTime > time + view_field + view_field_tolerance)
                        continue;

                    var ubhelper = new UbNoteBuilder(obj);

                    var iconType = ubhelper.InferObjectTypeIcon();

                    int col = note.Column;

                    if (col == 4)
                    {
                        if (iconType == UbIconType.Zoom)
                            zoomedIn = !zoomedIn;
                        else
                            flippedRight = !flippedRight;

                        if (note.StartTime > time) continue;

                        if (iconType == UbIconType.Zoom)
                        {
                            olZoomedIn = !olZoomedIn;
                        }
                        else
                        {
                            olFlippedRight = !olFlippedRight;
                        }
                    }
                }

                double startTime = note.StartTime;
                bool isHold = note is IHasDuration;
                double endTime = note is IHasDuration duration ? startTime + duration.Duration : startTime;


                // Check if this note is being hit (within hit window)
                if (column >= 0 && column < 4 && Math.Abs(time - startTime) < hit_window)
                {
                    int innerCircleIndex = getInnerCircleIndexForColumnAndFlip(column, flippedRight);
                    if (innerCircleIndex >= 0)
                    {
                        hitCircles[innerCircleIndex].FlashColour(colourProvider.Background1, (float)flash_duration);
                    }
                }

                if (isHold && endTime > time && startTime < time + view_field + view_field_tolerance)
                {
                    var pos = GetPreviewNotePosition(column, startTime, time, flippedRight, zoomedIn);

                    var ubhelper = new UbNoteBuilder(note);
                    var iconType = ubhelper.InferObjectTypeIcon();

                    int endColumn = column;
                    if (iconType == UbIconType.Double)
                        endColumn = getDoubleEndLane(column);

                    var tailPos = GetPreviewNotePosition(endColumn, endTime, time, flippedRight, zoomedIn);

                    // diagonal for double holds
                    bool holdFlippedRight = flippedRight;
                    if (column < 2 && !zoomedIn) holdFlippedRight = !holdFlippedRight;

                    if (iconType == UbIconType.Double)
                    {
                        float startY = pos.Y;
                        float endY = tailPos.Y;

                        double holdDuration = endTime - startTime;
                        float speedX = holdFlippedRight
                            ? (float)((1.0 - right_receptor) / view_field)
                            : -(float)(left_receptor / view_field);
                        float D = (float)holdDuration * speedX;

                        if (Math.Abs(D) > 0.0001f)
                        {
                            float deltaY = endY - startY;
                            float slope = deltaY / D;

                            float clampedX = holdFlippedRight
                                ? Math.Max(pos.X, right_receptor)
                                : Math.Min(pos.X, left_receptor);
                            float lineStartY = tailPos.Y - slope * (tailPos.X - clampedX);
                            var lineStart = new Vector2(clampedX, lineStartY);

                            var previewHold = getPooledHold();
                            if (notesLayer.DrawSize != Vector2.Zero)
                                previewHold.SetDiagonal(lineStart, tailPos, notesLayer.DrawSize);
                            previewHold.Show();

                            if (time >= startTime)
                            {
                                var previewNote2 = getPooledNote();
                                previewNote2.SetIconType(iconType);
                                previewNote2.Position = lineStart;
                                previewNote2.Show();
                            }
                        }
                    }
                    else
                    {
                        // clamp hold start to corresponding receptor
                        if (holdFlippedRight)
                            pos.X = Math.Max(pos.X, right_receptor);
                        else
                            pos.X = Math.Min(pos.X, left_receptor);

                        var holdVisualStart = holdFlippedRight ? pos : tailPos;
                        var holdVisualEnd = holdFlippedRight ? tailPos : pos;

                        var previewHold = getPooledHold();
                        previewHold.Position = holdVisualStart;
                        previewHold.EndPosition = holdVisualEnd;

                        if (!holdFlippedRight)
                            previewHold.Width = Math.Min(previewHold.Width, left_receptor - previewHold.X);

                        previewHold.Show();

                        if (Math.Abs(pos.X - (holdFlippedRight ? right_receptor : left_receptor)) < 0.01f)
                        {
                            var previewNote2 = getPooledNote();
                            previewNote2.SetIconType(iconType);
                            previewNote2.Position = pos;
                            previewNote2.Show();
                        }
                    }

                    var endNote = getPooledNote();
                    endNote.SetIconType(iconType);
                    notesLayer.Remove(endNote, false);
                    endNote.Depth = 11;
                    notesLayer.Add(endNote);
                    endNote.Scale = new Vector2(0.5f);
                    endNote.Position = tailPos;
                    endNote.Show();

                    // Flash the receptor on the opposite lane when a Double hold ends
                    if (iconType == UbIconType.Double && Math.Abs(time - endTime) < hit_window)
                    {
                        int endInnerCircle = getInnerCircleIndexForColumnAndFlip(endColumn, holdFlippedRight);
                        if (endInnerCircle >= 0)
                            hitCircles[endInnerCircle].FlashColour(colourProvider.Background1, (float)flash_duration);
                    }
                }

                // Continue search until window is reached
                if (note.StartTime < time) continue;

                // Putting camera update in here because this about the time where we are
                // close to the current time
                if (!camUpdated)
                {
                    camUpdated = true;

                    if (olZoomedIn)
                    {
                        if (centerForUpcomingFlip)
                        {
                            // Upcoming flip stage: nudge slightly inward from the current side instead of centering.
                            float inwardPull = olFlippedRight ? 0.04f : -0.04f;

                            cameraBorder.MoveTo(new Vector2(cam_middle_receptor + inwardPull, cam_middle_receptor), 700,
                                Easing.OutCubic);
                            cameraBorder.ResizeTo(new Vector2(preview_width * 0.52f, preview_height * 0.79f), 700,
                                Easing.OutCubic);
                        }
                        else if (olFlippedRight)
                        {
                            cameraBorder.MoveTo(new Vector2(cam_right_receptor, cam_middle_receptor), 700,
                                Easing.OutCubic);
                            cameraBorder.ResizeTo(new Vector2(preview_width * 0.48f, preview_height * 0.75f), 700,
                                Easing.OutCubic);
                        }
                        else
                        {
                            cameraBorder.MoveTo(new Vector2(cam_left_receptor, cam_middle_receptor), 700,
                                Easing.OutCubic);
                            cameraBorder.ResizeTo(new Vector2(preview_width * 0.48f, preview_height * 0.75f), 700,
                                Easing.OutCubic);
                        }
                    }
                    else
                    {
                        cameraBorder.MoveTo(new Vector2(cam_middle_receptor, cam_middle_receptor), 700,
                            Easing.OutCubic);
                        cameraBorder.ResizeTo(new Vector2(preview_width * 0.7f, preview_height * 0.90f), 700,
                            Easing.OutCubic);
                    }

                    indicatorLayer.UpdateIndicators(olFlippedRight, !olZoomedIn);
                }

                if (note.StartTime > time + view_field + view_field_tolerance) break;


                if (column == 4)
                    continue;

                var previewNote = makeNote(note);
                previewNote.Position = GetPreviewNotePosition(column, startTime, time, flippedRight, zoomedIn);

                if (previewNote.IconType == UbIconType.Dodge)
                {
                    if (column == 2 || column == 0)
                    {
                        // Flip spike when they are on top lane
                        previewNote.Scale = new Vector2(previewNote.Scale.X, -previewNote.Scale.Y);
                    }
                }

                previewNote.Show();
            }
        }

        private (bool flippedRight, bool zoomedIn) getCameraStateAtTime(double currentTime)
        {
            bool flippedRight = true;
            bool zoomedIn = true;

            foreach (var obj in editorBeatmap.HitObjects)
            {
                if (obj is not ManiaHitObject note || note.Column != 4)
                    continue;

                if (note.StartTime > currentTime)
                    break;

                var ubhelper = new UbNoteBuilder(note);
                var iconType = ubhelper.InferObjectTypeIcon();

                if (iconType == UbIconType.Zoom)
                    zoomedIn = !zoomedIn;
                else
                    flippedRight = !flippedRight;
            }

            return (flippedRight, zoomedIn);
        }

        private bool shouldCenterForUpcomingFlip(double currentTime)
        {
            foreach (var obj in editorBeatmap.HitObjects)
            {
                if (obj is not ManiaHitObject note || note.Column != 4)
                    continue;

                if (note.StartTime < currentTime)
                    continue;

                if (note.StartTime > currentTime + view_field + view_field_tolerance)
                    break;

                var ubhelper = new UbNoteBuilder(note);
                if (ubhelper.InferObjectTypeIcon() != UbIconType.Flip)
                    continue;

                if (ubhelper.InferObjectModifierIcons().Contains(UbIconType.ModSwapImmediate))
                    continue;

                double twoBeats = editorBeatmap.GetBeatLengthAtTime(note.StartTime) * 2;
                if (note.StartTime - currentTime <= twoBeats)
                    return true;
            }

            return false;
        }

        private int getInnerCircleIndexForColumnAndFlip(int column, bool flippedRight)
        {
            // Inner circles are at odd indices
            if (column == 2 || column == 0) // Top column
            {
                return flippedRight ? 5 : 1; // right or left top inner circle
            }
            else if (column == 3 || column == 1) // Bottom column
            {
                return flippedRight ? 7 : 3; // right or left bottom inner circle
            }

            return -1;
        }

        private PreviewNote getPooledNote()
        {
            if (activeNoteCount < notePool.Count)
            {
                var note = notePool[activeNoteCount++];
                note.Reset();
                return note;
            }

            var newNote = new PreviewNote();
            notePool.Add(newNote);
            notesLayer.Add(newNote);
            activeNoteCount++;
            return newNote;
        }

        private PreviewHold getPooledHold()
        {
            if (activeHoldCount < holdPool.Count)
            {
                var hold = holdPool[activeHoldCount++];
                hold.Reset();
                return hold;
            }

            var newHold = new PreviewHold();
            holdPool.Add(newHold);
            notesLayer.Add(newHold);
            activeHoldCount++;
            return newHold;
        }

        private PreviewNote makeNote(ManiaHitObject hitObject)
        {
            var note = getPooledNote();
            var ubiconHelper = new UbNoteBuilder(hitObject);
            note.SetIconType(ubiconHelper.InferObjectTypeIcon());
            return note;
        }

        private Vector2 GetPreviewNotePosition(int column, double hitTime, double currentTime, bool flippedRight, bool zoomedIn)
        {
            var vector = new Vector2(0);

            if (column <= 1 && !zoomedIn)
            {
                flippedRight = !flippedRight;
            }
            
            if (flippedRight)
            {
                vector.X = (float)Map(hitTime, currentTime, currentTime + view_field, right_receptor, 1);
            }
            else
            {
                vector.X = (float)Map(hitTime, currentTime, currentTime + view_field, left_receptor, 0);
            }

            if (column == 2 || column == 0)
            {
                vector.Y = top_receptor;
            }
            else if (column == 3 || column == 1)
            {
                vector.Y = bottom_receptor;
            }
            else if (column == 5)
            {
                vector.Y = middle_receptor;
            }

            return vector;
        }

        public double Map(double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            return fromTarget + (value - fromSource) * (toTarget - fromTarget) / (toSource - fromSource);
        }
    }
}
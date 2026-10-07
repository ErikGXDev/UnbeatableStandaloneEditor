using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit.Preview
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

        private double viewField => 800 * viewFieldMultiplier;
        private double viewFieldTolerance => 400 * viewFieldMultiplier;

        private double viewFieldMultiplier = 1f;
        
        private double pViewFieldMultiplier = 1f;
        public double ViewFieldMultiplier
        {
            get => pViewFieldMultiplier;
            set {
                if (config != null)
                {
                    config.SetValue(OsuSetting.EditorPreviewZoom, value);
                }
                pViewFieldMultiplier = value;
            }
        }
        
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;


        private ExpandingToolboxContainer rightToolbox = null!;

        private Container notesLayer = null!;
        private Container decoLayer = null!;
        private PreviewIndicator indicatorLayer = null!;
        private PreviewCopIndicator copIndicatorLayer = null!;
        private Box cameraBorder;

        private bool chartHasBrawls;
        private readonly bool[] copAlive = new bool[PreviewCopIndicator.COP_COUNT];
        private readonly List<CopSpan>[] copSpans = CreateCopSpans();

        private List<Circle> hitCircles = new List<Circle>();
        private readonly List<Circle> copHitCircles = new List<Circle>();

        private readonly UbNoteBuilder noteBuilder = new UbNoteBuilder(null);

        private List<PreviewNote> notePool = new List<PreviewNote>();
        private List<PreviewHold> holdPool = new List<PreviewHold>();
        private int activeNoteCount;
        private int activeHoldCount;

        private const double flash_duration = 150;
        private const double hit_window = 50;

        
        private const float cop_column_offset = 0.16f;
        private const float cop_travel_distance = 0.75f;
        private const float brawl_camera_zoom = 0.925f;

        
        private const float brawl_preview_taller = 1.15f;
        
        private const float cop_indicator_inset = 8;

        private const float cop_goal_offset = 0.00f;

        [Resolved] private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved] private IEditorChangeHandler? changeHandler { get; set; }

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
        private void load()
        {
            ViewFieldMultiplier = config.Get<double>(OsuSetting.EditorPreviewZoom);
            viewFieldMultiplier = ViewFieldMultiplier;
            
            InternalChildren = new[]
            {
                indicatorLayer = new PreviewIndicator
                {
                    RelativePositionAxes = Axes.X,
                    Position = new Vector2(0.5f, -8),
                    Depth = 2
                },
                copIndicatorLayer = new PreviewCopIndicator
                {
                    RelativePositionAxes = Axes.X,
                    Position = new Vector2(1f, -8),
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
            
            if (changeHandler != null)
                changeHandler.OnStateChange += onEditorStateChanged;

            recomputeCopState();
        }

        private void onEditorStateChanged() => recomputeCopState();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (!isDisposing)
                return;

            if (changeHandler != null)
                changeHandler.OnStateChange -= onEditorStateChanged;
        }
        
        // Tracks when cops are dead/alive across whole beatmap
        private readonly record struct CopSpan(double Start, double End);

        private static List<CopSpan>[] CreateCopSpans() =>
            Enumerable.Range(0, PreviewCopIndicator.COP_COUNT).Select(_ => new List<CopSpan>()).ToArray();
        
        private void recomputeCopState()
        {
            foreach (var spans in copSpans)
                spans.Clear();

            chartHasBrawls = false;

            foreach (var obj in editorBeatmap.HitObjects)
            {
                if (obj is not ManiaHitObject note)
                    continue;

                var builder = new UbNoteBuilder(note);
                if (builder.InferObjectTypeIcon() != UbIconType.Brawl)
                    continue;

                chartHasBrawls = true;

                var modifiers = builder.InferObjectModifierIcons();

                int cop = copIndexFrom(modifiers);
                if (cop < 0)
                    continue;

                var spans = copSpans[cop];
                
                double endTime = note.GetEndTime();

                bool isAlive = spans.Count > 0 && double.IsPositiveInfinity(spans[spans.Count - 1].End);

                if (modifiers.Contains(UbIconType.ModCopFinish))
                {
                    if (isAlive)
                        spans[spans.Count - 1] = new CopSpan(spans[spans.Count - 1].Start, endTime);
                }
                else if (!isAlive)
                {
                    spans.Add(new CopSpan(note.StartTime, double.PositiveInfinity));
                }
            }

            copIndicatorLayer.FadeTo(chartHasBrawls ? 1 : 0, 200, Easing.OutQuint);
        }

        private static int copIndexFrom(List<UbIconType> modifiers)
        {
            if (modifiers.Contains(UbIconType.ModCop1)) return 0;
            if (modifiers.Contains(UbIconType.ModCop2)) return 1;
            if (modifiers.Contains(UbIconType.ModCop3)) return 2;
            if (modifiers.Contains(UbIconType.ModCop4)) return 3;
            return -1;
        }

        private void updateCopLiveness(double time)
        {
            for (int i = 0; i < copAlive.Length; i++)
                copAlive[i] = copSpans[i].Any(span => time >= span.Start && time < span.End);

            copIndicatorLayer.UpdateCops(copAlive);
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
            
            foreach (float x in new[] { left_receptor - cop_column_offset, right_receptor + cop_column_offset })
            {
                foreach (float y in new[] { top_receptor + cop_goal_offset, bottom_receptor - cop_goal_offset })
                {
                    foreach (var pair in sizePairs)
                    {
                        var copCircle = new Circle
                        {
                            Colour = pair.Item2,
                            RelativePositionAxes = Axes.Both,
                            Position = new Vector2(x, y),
                            Size = new Vector2(pair.Item1 - cop_indicator_inset),
                            Origin = Anchor.Centre,
                            Alpha = 0,
                        };

                        decoLayer.Add(copCircle);
                        copHitCircles.Add(copCircle);
                    }
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

        private int getLowLane()
        {
            return composer.Is4Key ? 1 : 3;
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

            if (chartHasBrawls)
                updateCopLiveness(clock.CurrentTime);
            
            // Lerp viewFieldMultiplier towards ViewFieldMultiplier
            viewFieldMultiplier = Interpolation.Lerp(viewFieldMultiplier, ViewFieldMultiplier, 0.05f);

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

            bool soonestNoteRecorded = false;
            bool soonestIsBrawl = false;
            bool activeBrawlHold = false;


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
                    if (note.StartTime > time + viewField + viewFieldTolerance)
                        continue;

                    var iconType = getIconType(note);

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
                double endTime = note.GetEndTime();

                bool isBrawl = getIconType(note) == UbIconType.Brawl;

                // Cop holds are always drawn on the low lane unless heavy
                int brawlColumn = isBrawl && isForcedLowBrawl(column, note) ? getLowLane() : column;


                // Check if this note is being hit (within hit window)
                if (column >= 0 && column < 4 && Math.Abs(time - startTime) < hit_window)
                {
                    int innerCircleIndex = getInnerCircleIndexForColumnAndFlip(isBrawl ? brawlColumn : column, flippedRight);
                    if (innerCircleIndex >= 0)
                    {
                        hitCircles[innerCircleIndex].FlashColour(colourProvider.Background1, (float)flash_duration);
                    }
                }

                if (isHold && endTime > time && startTime < time + viewField + viewFieldTolerance)
                {
                    var iconType = getIconType(note);

                    if (isBrawl && endTime > time)
                        activeBrawlHold = true;

                    int endColumn = isBrawl ? brawlColumn : column;

                    if (iconType == UbIconType.Double)
                        endColumn = getDoubleEndLane(column);

                    Vector2 getPosition(int col, double t) => isBrawl
                        ? GetBrawlNotePosition(col, t, time, flippedRight, zoomedIn)
                        : GetPreviewNotePosition(col, t, time, flippedRight, zoomedIn);

                    var pos = getPosition(isBrawl ? brawlColumn : column, startTime);
                    var tailPos = getPosition(endColumn, endTime);

                    // diagonal for double holds
                    bool holdFlippedRight = flippedRight;
                    if (column < 2 && !zoomedIn) holdFlippedRight = !holdFlippedRight;

                    if (isBrawl || iconType == UbIconType.Double)
                    {
                        var holdStart = pos;
                        bool drawDiagonal = true;

                        if (!isBrawl)
                        {
                            double holdDuration = endTime - startTime;
                            float speedX = holdFlippedRight
                                ? (float)((1.0 - right_receptor) / viewField)
                                : -(float)(left_receptor / viewField);
                            float D = (float)holdDuration * speedX;

                            drawDiagonal = Math.Abs(D) > 0.0001f;

                            if (drawDiagonal)
                            {
                                float slope = (tailPos.Y - pos.Y) / D;
                                float clampedX = holdFlippedRight
                                    ? Math.Max(pos.X, right_receptor)
                                    : Math.Min(pos.X, left_receptor);

                                holdStart = new Vector2(clampedX, tailPos.Y - slope * (tailPos.X - clampedX));
                            }
                        }

                        if (drawDiagonal)
                        {
                            var previewHold = getPooledHold();
                            if (notesLayer.DrawSize != Vector2.Zero)
                                previewHold.SetDiagonal(holdStart, tailPos, notesLayer.DrawSize);
                            previewHold.Show();

                            if (time >= startTime)
                            {
                                var previewNote2 = getPooledNote();
                                previewNote2.SetIconType(iconType);
                                previewNote2.Position = holdStart;
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

                // check if closest is brawl
                if (!soonestNoteRecorded && column != 4 && note.StartTime <= time + viewField)
                {
                    soonestNoteRecorded = true;
                    soonestIsBrawl = getIconType(note) == UbIconType.Brawl;
                }

                // Putting camera update in here because this about the time where we are
                // close to the current time
                if (!camUpdated)
                {
                    camUpdated = true;

                    updateCamera(olZoomedIn, olFlippedRight, centerForUpcomingFlip, soonestIsBrawl || activeBrawlHold);

                    indicatorLayer.UpdateIndicators(olFlippedRight, !olZoomedIn, centerForUpcomingFlip);
                }

                if (note.StartTime > time + viewField + viewFieldTolerance) break;


                if (column == 4)
                    continue;

                var previewNote = makeNote(note);

                if (previewNote.IconType == UbIconType.Brawl)
                    previewNote.Position = GetBrawlNotePosition(isForcedLowBrawl(column, note) ? getLowLane() : column,
                        startTime, time, flippedRight, zoomedIn);
                else
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

            bool brawlPull = soonestIsBrawl || activeBrawlHold;

            if (!camUpdated)
            {
                updateCamera(olZoomedIn, olFlippedRight, centerForUpcomingFlip, brawlPull);
                indicatorLayer.UpdateIndicators(olFlippedRight, !olZoomedIn, centerForUpcomingFlip);
            }

            // Update cop circles
            for (int i = 0; i < copHitCircles.Count; i++)
                copHitCircles[i].FadeTo(brawlPull && (i < 4) == !olFlippedRight ? 1 : 0, 200, Easing.OutQuint);
            
            // Update height of preview
            this.ResizeTo(new Vector2(preview_width, preview_height * (brawlPull ? brawl_preview_taller : 1)), 700, Easing.OutQuint);
        }

        private void updateCamera(bool zoomedIn, bool flippedRight, bool centerForUpcomingFlip, bool brawlPull)
        {
            float x, width, height;

            if (brawlPull)
            {
                x = flippedRight ? right_receptor + cop_column_offset : left_receptor - cop_column_offset;
                width = preview_width * 0.48f * brawl_camera_zoom;
                height = preview_height * 0.9f * brawl_camera_zoom;
            }
            else if (!zoomedIn)
            {
                x = cam_middle_receptor;
                width = preview_width * 0.7f;
                height = preview_height * 0.90f;
            }
            else if (centerForUpcomingFlip)
            {
                x = cam_middle_receptor + (flippedRight ? 0.04f : -0.04f);
                width = preview_width * 0.52f;
                height = preview_height * 0.79f;
            }
            else
            {
                x = flippedRight ? cam_right_receptor : cam_left_receptor;
                width = preview_width * 0.48f;
                height = preview_height * 0.75f;
            }

            cameraBorder.MoveTo(new Vector2(x, cam_middle_receptor), 700, Easing.OutQuint);
            cameraBorder.ResizeTo(new Vector2(width, height), 700, Easing.OutQuint);
        }
        
        private bool shouldCenterForUpcomingFlip(double currentTime)
        {
            
            var timingPoint = editorBeatmap.ControlPointInfo.TimingPointAt(currentTime);
            var twoBeats = timingPoint.BeatLength * 2.0D;
            
            List<HitObject> zoomTimes = new List<HitObject>();
            foreach (var obj in editorBeatmap.HitObjects)
            {
                if (obj is not ManiaHitObject note || note.Column != 4)
                    continue;

                if (note.StartTime < currentTime)
                    continue;

                if (note.StartTime > currentTime + viewField + viewFieldTolerance * 2 + twoBeats)
                    break;


                var flipIconType = getIconType(note);
                if (flipIconType == UbIconType.Zoom)
                {
                    zoomTimes.Add(note);
                    continue;
                }

                if (flipIconType != UbIconType.Flip)
                    continue;

                if (noteBuilder.InferObjectModifierIcons().Contains(UbIconType.ModSwapImmediate))
                    continue;
                
                //double twoBeats = editorBeatmap.ControlPointInfo.TimingPointAt(note.StartTime).BeatLength * 2.0D;
                
                var probablyCenter = note.StartTime - currentTime <= twoBeats;

                if (probablyCenter)
                {
              
                    foreach (var zoom in zoomTimes)
                    {
                        if (zoom.StartTime > currentTime && zoom.StartTime <= note.StartTime)
                        {
                            var zoomIndex = editorBeatmap.FindIndex(zoom);
                            var flipIndex = editorBeatmap.FindIndex(note);
                            
                            // Only zoom out if the zoom is after the flip in the beatmap order
                            if (zoomIndex > flipIndex)
                            {
                                return true;
                            }
                            
                            return false;
                        }
                    }
                    
                    return true;
                }
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
        
        private UbIconType getIconType(HitObject note)
        {
            noteBuilder.ChangeHitObject(note);
            return noteBuilder.InferObjectTypeIcon();
        }

        private PreviewNote makeNote(ManiaHitObject hitObject)
        {
            var note = getPooledNote();

            var iconType = getIconType(hitObject);

            // Extra logic to detect smaller freestyle notes
            if (iconType == UbIconType.Freestyle)
            {
                int index = editorBeatmap.FindIndex(hitObject);
                if (index > 0)
                {
                    var columns = new[] { 2, 3 };
                    if (composer.Is4Key)
                        columns = [0, 1, 2, 3];
                    
                    for (int i = index - 1; i >= 0; i--)
                    {
                        var prevObj = editorBeatmap.HitObjects[i];
                        if (prevObj is ManiaHitObject prevNote)
                        {
                            var prevIconType = getIconType(prevNote);

                            if (columns.Contains(prevNote.Column))
                            {
                                if (prevIconType == UbIconType.Animated || prevIconType == UbIconType.AnimatedHold)
                                {
                                    continue; // Skip Animated notes, even though they're in columns that should usually interrupt small freestyles
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (prevIconType == UbIconType.Flip)
                            {
                                break;
                            }
                            
                            if (prevIconType == UbIconType.Freestyle)
                            {
                                iconType = UbIconType.FreestyleSmall;
                                break;
                            }
                        }
                    }
                }
            }
            
            
            note.SetIconType(iconType);
            
            
            
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
                vector.X = (float)Map(hitTime, currentTime, currentTime + viewField, right_receptor, 1);
            }
            else
            {
                vector.X = (float)Map(hitTime, currentTime, currentTime + viewField, left_receptor, 0);
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
        
        private bool isForcedLowBrawl(int column, ManiaHitObject note)
        {
            if (column != 0 && column != 2)
                return false;

            if (note is not IHasDuration)
                return false;

            noteBuilder.ChangeHitObject(note);
            return !noteBuilder.InferObjectModifierIcons().Contains(UbIconType.ModCopHeavy);
        }
        
        private Vector2 GetBrawlNotePosition(int column, double hitTime, double currentTime, bool flippedRight, bool zoomedIn)
        {
            var pos = GetPreviewNotePosition(column, hitTime, currentTime, flippedRight, zoomedIn);
            
            if (column <= 1 && !zoomedIn)
                flippedRight = !flippedRight;

            pos.X = flippedRight ? right_receptor + cop_column_offset : left_receptor - cop_column_offset;

            if (column != 5)
            {
                double travel = Math.Max(0, hitTime - currentTime);
                float progress = (float)Math.Clamp(travel / viewField, 0, 1);
                
                if (column == 2 || column == 0)
                {
                    pos.Y = top_receptor + cop_goal_offset - cop_travel_distance * progress;
                }
                else
                {
                    pos.Y = bottom_receptor - cop_goal_offset + cop_travel_distance * progress;
                }
            }

            return pos;
        }

        public double Map(double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            return fromTarget + (value - fromSource) * (toTarget - fromTarget) / (toSource - fromSource);
        }

        public override bool HandlePositionalInput { get; } = true;
    }
}
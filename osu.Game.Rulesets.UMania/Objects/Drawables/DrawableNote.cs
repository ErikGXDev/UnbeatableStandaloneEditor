// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Rulesets.UMania.Configuration;
using osu.Game.Rulesets.UMania.Edit;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Skinning;
using osu.Game.Rulesets.UMania.Skinning.Default;
using osu.Game.Screens.Edit;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.UMania.Objects.Drawables
{
    /// <summary>
    /// Visualises a <see cref="Note"/> hit object.
    /// </summary>
    public partial class DrawableNote : DrawableManiaHitObject<Note>, IKeyBindingHandler<ManiaAction>
    {
        [Resolved]
        private OsuColour colours { get; set; }

        [Resolved(canBeNull: true)]
        private IBeatmap beatmap { get; set; }

        private readonly Bindable<bool> configTimingBasedNoteColouring = new Bindable<bool>();

        protected virtual ManiaSkinComponents Component => ManiaSkinComponents.Note;

        private Drawable headPiece;

        public DrawableNote()
            : this(null)
        {
        }

        public DrawableNote(Note hitObject)
            : base(hitObject)
        {
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader(true)]
        private void load(ManiaRulesetConfigManager rulesetConfig)
        {
            rulesetConfig?.BindWith(ManiaRulesetSetting.TimingBasedNoteColouring, configTimingBasedNoteColouring);

            AddInternal(headPiece =
                new SkinnableDrawable(new ManiaSkinComponentLookup(Component), _ => new DefaultNotePiece())
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                });

            HitObjectApplied += _ => addIcon();
        }

        [CanBeNull]
        private Drawable noteSprite;

        private List<Drawable> modIcons = new List<Drawable>();

        [Resolved(null, null, true)]
        private UnbeatableHitObjectComposer composer { get; set; } = null!;

        private void addIcon()
        {
            if (composer == null)
            {
                return;
            }

            if (this is not DrawableHoldNoteTail && HitObject != null)
            {
                var helper = new UbNoteBuilderHelper(composer, HitObject);

                var icon = helper.InferObjectTypeIcon();

                if (noteSprite != null)
                    noteSprite.Expire();

                Logger.Log("Drawing a " + icon + " icon on note");
                AddInternal(new Container
                {
                    Child = noteSprite = new UbIcon(icon)
                    {
                        Scale = new Vector2(2f),
                    }
                });

                if (modIcons.Count > 0)
                {
                    foreach (var modIcon in modIcons)
                        modIcon.Expire();
                }

                var infIcons = helper.InferObjectModifierIcons();

                if (infIcons.Count > 0)
                {
                    var scale = infIcons.Count > 2 ? 0.85f : 1f;
                    var xOffset = infIcons.Count > 2 ? -5 : 0;

                    Drawable dr = new Container
                    {
                        X = xOffset,
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(2),
                                AutoSizeAxes = Axes.Both,
                                Children = infIcons.ConvertAll(i => new UbIcon(i)
                                {
                                    Scale = new Vector2(scale),
                                })
                            }
                        }
                    };
                    AddInternal(dr);
                    modIcons.Add(dr);
                }
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            configTimingBasedNoteColouring.BindValueChanged(_ => updateSnapColour());
            StartTimeBindable.BindValueChanged(_ => updateSnapColour(), true);
        }

        protected override void OnApply()
        {
            base.OnApply();
            updateSnapColour();
        }

        protected override void OnDirectionChanged(ValueChangedEvent<ScrollingDirection> e)
        {
            base.OnDirectionChanged(e);

            headPiece.Anchor = headPiece.Origin =
                e.NewValue == ScrollingDirection.Up ? Anchor.TopCentre : Anchor.BottomCentre;
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            Debug.Assert(HitObject.HitWindows != null);

            if (!userTriggered)
            {
                if (!HitObject.HitWindows.CanBeHit(timeOffset))
                    ApplyMinResult();

                return;
            }

            var result = HitObject.HitWindows.ResultFor(timeOffset);

            if (result == HitResult.None)
                return;

            result = GetCappedResult(result);
            ApplyResult(result);
        }

        /// <summary>
        /// Some objects in mania may want to limit the max result.
        /// </summary>
        protected virtual HitResult GetCappedResult(HitResult result) => result;

        public virtual bool OnPressed(KeyBindingPressEvent<ManiaAction> e)
        {
            if (e.Action != Action.Value)
                return false;

            if (CheckHittable?.Invoke(this, Time.Current) == false)
                return false;

            return UpdateResult(true);
        }

        public virtual void OnReleased(KeyBindingReleaseEvent<ManiaAction> e)
        {
        }

        private void updateSnapColour()
        {
            if (beatmap == null || HitObject == null) return;

            int snapDivisor = beatmap.ControlPointInfo.GetClosestBeatDivisor(HitObject.StartTime);

            Colour = configTimingBasedNoteColouring.Value
                ? BindableBeatDivisor.GetColourFor(snapDivisor, colours)
                : Color4.White;
        }
    }
}

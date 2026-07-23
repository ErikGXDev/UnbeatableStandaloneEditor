// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UMania.Objects.Drawables;
using osu.Game.Rulesets.UMania.UI;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit
{
    public partial class EditorColumn : Column
    {
        public EditorColumn(int index, bool isSpecial)
            : base(index, isSpecial)
        {
        }

        [Resolved] private UnbeatableHitObjectComposer composer { get; set; } = null!;

        private SpriteText labelText = null!;
        private SpriteText keyLabel = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            string? label = Index switch
            {
                0 => "Top F",
                1 => "Bottom F",
                2 => "Top",
                3 => "Bottom",
                4 => "Camera",
                5 => "Middle",
                _ => null,
            };

            if (label != null)
            {
                TopLevelContainer.Add(labelText = new SpriteText
                {
                    Text = label,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Y = -3,
                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                    Colour = Colour4.White.Opacity(0.7f),
                    Shadow = true,
                    ShadowColour = Colour4.Black
                });
            }

            var is4Key = composer.Is4Key;
            if (!is4Key && (Index == 0 || Index == 1))
            {
                labelText.Alpha = 0;
            }
            
            TopLevelContainer.Add(keyLabel = new SpriteText
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                Y = -50,
                Font = OsuFont.GetFont(size: 20, weight: FontWeight.Bold),
                Colour = Colour4.White,
                Shadow = true,
                ShadowColour = Colour4.Black,
                Alpha = 0,
            });

            updateKeyLabel();
            composer.KeyBasedCharting.BindValueChanged(_ => updateKeyLabel());
        }

        private void updateKeyLabel()
        {
            if (!composer.KeyBasedCharting.Value)
            {
                keyLabel.Alpha = 0;
                return;
            }

            // Reverse the column mapping to find which key (1-6) targets this column.
            int[] mapping = composer.Is4Key
                ? new[] { 0, 2, 3, 1, 4, 5 }
                : new[] { 0, 1, 2, 3, 4, 5 };

            for (int keyIndex = 0; keyIndex < mapping.Length; keyIndex++)
            {
                if (mapping[keyIndex] == Index)
                {
                    keyLabel.Text = (keyIndex + 1).ToString();
                    keyLabel.Alpha = 1;
                    return;
                }
            }

            keyLabel.Alpha = 0;
        }

        protected override void OnNewDrawableHitObject(DrawableHitObject drawableHitObject)
        {
            base.OnNewDrawableHitObject(drawableHitObject);
            drawableHitObject.ApplyCustomUpdateState += (dho, state) =>
            {
                switch (dho)
                {
                    // hold note heads are exempt from what follows due to the "freezing" mechanic
                    // which already ensures they'll never fade away on their own.
                    case DrawableHoldNoteHead:
                        break;

                    // mania features instantaneous hitobject fade-outs.
                    // this means that without manual intervention stopping the clock at the precise time of hitting the object
                    // means the object will fade out.
                    // this is anti-user in editor contexts, as the user is expecting to continue the see the note on the receptor line.
                    // therefore, apply a crude workaround to prevent it from going away.
                    default:
                    {
                        if (state == ArmedState.Hit)
                            dho.FadeTo(1).Delay(1).FadeOut().Expire();
                        break;
                    }
                }
            };
        }
    }
}

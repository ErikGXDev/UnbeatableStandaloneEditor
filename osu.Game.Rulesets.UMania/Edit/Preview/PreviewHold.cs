using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;
using System;

namespace osu.Game.Rulesets.UMania.Edit
{
    public partial class PreviewHold : CompositeDrawable
    {
        private Box line = null!;

        public PreviewHold()
        {
            RelativePositionAxes = Axes.Both;
            RelativeSizeAxes = Axes.X;
            Origin = Anchor.CentreLeft;
            Size = new Vector2(0, 4);
            Depth = 12;
        }

        public Vector2 EndPosition
        {
            set
            {
                float width = value.X - X;
                Y = value.Y;
                
                Width = width;
            }
        }

        
        public void SetDiagonal(Vector2 startRelative, Vector2 endRelative, Vector2 parentDrawSize)
        {
            RelativePositionAxes = Axes.None;
            RelativeSizeAxes = Axes.None;
            Origin = Anchor.CentreLeft;

            Vector2 startAbs = new Vector2(startRelative.X * parentDrawSize.X, startRelative.Y * parentDrawSize.Y);
            Vector2 endAbs = new Vector2(endRelative.X * parentDrawSize.X, endRelative.Y * parentDrawSize.Y);

            Vector2 delta = endAbs - startAbs;
            float distance = delta.Length;
            float angle = MathHelper.RadiansToDegrees(MathF.Atan2(delta.Y, delta.X));

            Position = startAbs;
            Size = new Vector2(distance, 4);
            Rotation = angle;
        }

        public void Reset()
        {
            RelativePositionAxes = Axes.Both;
            RelativeSizeAxes = Axes.X;
            Origin = Anchor.CentreLeft;
            Size = new Vector2(0, 4);
            Rotation = 0;
            Position = Vector2.Zero;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = line = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.LightBlue,
            };
        }
    }
}
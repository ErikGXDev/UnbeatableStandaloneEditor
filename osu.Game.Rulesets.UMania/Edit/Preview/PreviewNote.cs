using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.UMania.Edit
{
    public partial class PreviewNote : CompositeDrawable
    {
        private Drawable? shape;

        public PreviewNote()
        {
            RelativePositionAxes = Axes.Both;
            Origin = Anchor.Centre;
            Size = new Vector2(18);
            Depth = 10;
        }

        public UbIconType IconType
        {
            set => SetIconType(value);
            get => iconType;
        }

        private UbIconType iconType;

        public void SetIconType(UbIconType type)
        {
            bool needsTriangle = type == UbIconType.Dodge;
            bool hasTriangle = shape is Triangle;

            if (shape == null || needsTriangle != hasTriangle)
            {
                ClearInternal(true);
                shape = needsTriangle
                    ? new Triangle { RelativeSizeAxes = Axes.Both }
                    : new Circle { RelativeSizeAxes = Axes.Both };
                AddInternal(shape);
            }

            shape.Colour = colourFor(type);
            shape.Scale = type == UbIconType.Spam ? new Vector2(1, 1.5f) : Vector2.One;
            shape.Y = type == UbIconType.Spam ? -5 : 0;
            iconType = type;
        }

        public void Reset()
        {
            Scale = Vector2.One;
            Position = Vector2.Zero;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(shape = new Circle
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.White,
            });
        }

        private static Color4 colourFor(UbIconType type) => type switch
        {
            UbIconType.Hold => Color4.Snow,
            UbIconType.Double => Color4.CornflowerBlue,
            UbIconType.Dodge => Color4.Orange,
            UbIconType.Freestyle => Color4.Purple.Lighten(0.3f),
            UbIconType.Spam => Color4.DeepPink,
            UbIconType.Flip => Color4.Blue,
            UbIconType.Zoom => Color4.Orange,
            UbIconType.Brawl => Color4.Red,
            _ => new Color4(1f, 1f, 1f, 1f),
        };
    }
}
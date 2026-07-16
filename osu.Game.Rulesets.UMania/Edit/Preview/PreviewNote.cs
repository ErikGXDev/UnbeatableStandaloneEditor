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
        private Drawable shape = null!;

        public PreviewNote()
        {
            RelativePositionAxes = Axes.Both;
            Origin = Anchor.Centre;
            Size = new Vector2(18);
            Depth = 10;
        }

        public UbIconType IconType
        {
            set
            {
                iconType = value;
                if (shape != null)
                    shape.Colour = colourFor(value);
               
            }
            get => iconType;
        }

        private UbIconType iconType;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = shape = new Circle
            {
                RelativeSizeAxes = Axes.Both,
                
                Colour = colourFor(iconType),
            };
            
            if (iconType == UbIconType.Dodge)
            {
                InternalChild = shape = new Triangle()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourFor(iconType),
                };
            }

            if (iconType == UbIconType.Spam)
            {
                shape.Scale = new Vector2(1, 1.5f);
                shape.Y = -5;
            }
            
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
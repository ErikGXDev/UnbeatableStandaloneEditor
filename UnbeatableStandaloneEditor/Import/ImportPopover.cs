using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osuTK.Graphics;

namespace UnbeatableStandaloneEditor.Import;

public partial class ImportPopover : OsuPopover
{
    public ImportPopover() : base(false)
    { }

    private OsuFileSelector fileSelector = null!;

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider)
    {
        Child = new FillFlowContainer()
        {
            Direction = FillDirection.Vertical,
            Width = 600,
            Height = 400,
            Children = new Drawable[]
            {
                new OsuSpriteText()
                {
                    Text = "Import Beatmap Package (.zip)",
                    Font = OsuFont.Default.With(size: 18, weight: FontWeight.Bold),
                    Margin = new MarginPadding() { Left = 16, Top = 16, Bottom = 6},
                },
                fileSelector = new OsuFileSelector(validFileExtensions: new[] { ".zip", ".osz" })
                {
                    RelativeSizeAxes = Axes.Both,
                },
            }
        };

        Add(new Container
        {
            RelativeSizeAxes = Axes.Both,
            Masking = true,
            BorderThickness = 2,
            CornerRadius = 10,
            BorderColour = colourProvider.Highlight1,
            Children = new Drawable[]
            {
                new Box
                {
                    Colour = Color4.Transparent,
                    RelativeSizeAxes = Axes.Both,
                },
            }
        });
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        fileSelector.CurrentFile.BindValueChanged(file =>
        {
            if (file.NewValue != null)
            {
                Logger.Log($"Selected file for import: {file.NewValue.FullName}", LoggingTarget.Runtime, LogLevel.Important);

                // Hide popover after selection
                this.HidePopover();
            }
        });
    }
}

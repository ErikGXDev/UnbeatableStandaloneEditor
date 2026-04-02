using System.IO;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.IO.Archives;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Overlays.OSD;
using osuTK.Graphics;

namespace UnbeatableStandaloneEditor.Import;

public partial class ImportPopover : OsuPopover
{
    public ImportPopover() : base(false)
    { }

    private OsuFileSelector fileSelector = null!;

    [Resolved]
    private BeatmapManager beatmapManager { get; set; } = null!;

    [Resolved] private IAPIProvider api { get; set; } = null!;

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
                    Margin = new MarginPadding() { Left = 16, Top = 16, Bottom = 4},
                },
                new OsuSpriteText()
                {
                    AllowMultiline = true,
                    RelativeSizeAxes = Axes.X,
                    Text = "Select a .zip file and it will be imported automatically.",
                    Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular),
                    Colour = colourProvider.Content1.Opacity(0.75f),
                    Margin = new MarginPadding { Left = 16, Bottom = 6 },
                },
                fileSelector = new OsuFileSelector(validFileExtensions: new[] { ".zip" })
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

    [Resolved(canBeNull: true)] private OnScreenDisplay onScreenDisplay { get; set; }

    private partial class BeatmapEditorToast : Toast
    {
        public BeatmapEditorToast(LocalisableString value)
            : base(InputSettingsStrings.EditorSection, value)
        {
        }
    }

    private void showToast(string title)
    {
        onScreenDisplay?.Display(new BeatmapEditorToast(title));
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        fileSelector.CurrentFile.BindValueChanged(file =>
        {
            if (file.NewValue != null)
            {
                Task.Run(async () => await importBeatmap(file.NewValue.FullName));

                // Hide popover immediately
                this.HidePopover();
            }
        });
    }

    private Task importBeatmap(string filePath)
    {
        try
        {

            var archiveReader = new ProxyArchiveReader(filePath);
            Logger.Log(string.Join(",", archiveReader.Filenames));

            beatmapManager.Import(new BeatmapSetInfo(), archiveReader);

            Logger.Log($"Beatmap successfully added to database!");
            showToast("Imported package successfully!");
        }
        catch (Exception ex)
        {
            showToast("Import failed");
            Logger.Log($"Error during beatmap import: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}

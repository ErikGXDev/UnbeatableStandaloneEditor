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
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.IO.Archives;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Overlays.OSD;
using osuTK;
using osuTK.Graphics;

namespace UnbeatableStandaloneEditor.Import;

public partial class ImportPopover : OsuPopover, ICanAcceptFiles
{
    public ImportPopover() : base(false)
    {
    }

    private OsuFileSelector fileSelector = null!;

    [Resolved] private BeatmapManager beatmapManager { get; set; } = null!;

    [Resolved] private IAPIProvider api { get; set; } = null!;

    [Resolved]
    private OsuGameBase game { get; set; } = null!;

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider)
    {
        Child = new FillFlowContainer()
        {
            Direction = FillDirection.Vertical,
            Width = 600,
            Height = 355,
            Children = new Drawable[]
            {
                new FillFlowContainer()
                {
                    Size = new Vector2(600, 55),
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new OsuSpriteText()
                        {
                            Text = "Import Beatmap Package",
                            Font = OsuFont.Default.With(size: 18, weight: FontWeight.Bold),
                            Margin = new MarginPadding() { Left = 16, Top = 16, Bottom = 4 },
                        },
                        new OsuSpriteText()
                        {
                            AllowMultiline = true,
                            RelativeSizeAxes = Axes.X,
                            Text =
                                "Select a .zip, .osu or .txt file and it will be imported automatically. (or Drag and Drop while this is open!)",
                            Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular),
                            Colour = colourProvider.Content1.Opacity(0.75f),
                            Margin = new MarginPadding { Left = 16, Bottom = 6 },
                        },
                    }
                },
                new Container
                {
                    Size = new Vector2(600, 300),
                    Padding = new MarginPadding { Bottom = 1 },
                    Child = fileSelector = new OsuFileSelector(validFileExtensions: new[] { ".zip", ".txt", ".osu" })
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                }

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

        game.RegisterImportHandler(this);

    }

    public IEnumerable<string> HandledExtensions => new[] { ".zip", ".txt", ".osu" };


    public Task Import(params string[] paths)
    {
        return Task.Run(async () =>
        {
            foreach (var path in paths)
            {
                await importBeatmap(path);
            }
        });
    }


    public Task Import(ImportTask[] tasks, ImportParameters parameters = default)
    {
        return Task.Run(async () =>
        {
            foreach (var task in tasks)
            {
                await importBeatmap(task.Path);
            }
        });
    }


    private Task importBeatmap(string filePath)
    {
        try
        {

            if (filePath.EndsWith(".txt") || filePath.EndsWith(".osu"))
            {
                var archiveReader = new SingleFileArchiveReader(new List<string>() { filePath });
                Logger.Log(string.Join(",", archiveReader.Filenames));
                beatmapManager.Import(new BeatmapSetInfo(), archiveReader);
            }
            else if (filePath.EndsWith(".zip"))
            {
                var archiveReader = new ProxyArchiveReader(filePath);
                Logger.Log(string.Join(",", archiveReader.Filenames));

                beatmapManager.Import(new BeatmapSetInfo(), archiveReader);
            }


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

    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);

        game.UnregisterImportHandler(this);
    }
}

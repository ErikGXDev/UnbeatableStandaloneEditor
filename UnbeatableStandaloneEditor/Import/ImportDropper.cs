using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Overlays;

namespace UnbeatableStandaloneEditor.Import;

public partial class ImportDropper : Drawable, ICanAcceptFiles
{
    [Resolved]
    private OsuGameBase game { get; set; } = null!;

    [Resolved(canBeNull: true)] private OnScreenDisplay? onScreenDisplay { get; set; }

    [Resolved] private BeatmapManager beatmapManager { get; set; } = null!;


    public ImportDropper() {}

    protected override void LoadComplete()
    {
        base.LoadComplete();

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

    private void showToast(string title)
    {
        onScreenDisplay?.Display(new ImportPopover.BeatmapEditorToast(title));
    }

    private Task importBeatmap(string filePath)
    {

        if (BeatmapImporter.ImportBeatmap(filePath, beatmapManager))
        {
            showToast("Imported chart successfully!");
        }
        else
        {
            showToast("Import failed!");
        }

        return Task.CompletedTask;
    }


    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);

        game.UnregisterImportHandler(this);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Database;
using osu.Game.IO;
using osu.Game.IO.Archives;
using osu.Game.Localisation;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.OSD;
using osu.Game.Screens;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Custom
{
    public partial class TimingPointPasteButton : RoundedButton, ICanAcceptFiles
    {
        public TimingPointPasteButton() {}

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;
        
        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }
        
        [Resolved]
        private Clipboard hostClipboard { get; set; } = null!;
        
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;
        
        [Resolved(canBeNull: true)] private OnScreenDisplay? onScreenDisplay { get; set; }


        [BackgroundDependencyLoader]
        private void load()
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;
            Size = new Vector2(30);

            // Note to self: Do not use Child = ... because it removes the background of the button for some reason.
            Add(new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(16),
                Icon = FontAwesome.Solid.Clipboard,
                Depth = -1,
            });

            Action = () =>
            {
                try
                {
                    timingPointsOpening();
                } catch (Exception e)
                {
                    showToast("Could not paste from clipboard...");
                    Logger.Log($"Error while trying to paste osu! timing points: {e.Message}");
                }
            };
        }
        
        private void showToast(string title)
        {
            onScreenDisplay?.Display(new BeatmapEditorToast(title));
        }
        
        public partial class BeatmapEditorToast : Toast
        {
            public BeatmapEditorToast(LocalisableString value)
                : base(InputSettingsStrings.EditorSection, value)
            {
            }
        }

        private void timingPointsOpening()
        {
            var clipboard = hostClipboard.GetText();
            
            var corrected = "[General]\nMode: 5\n[TimingPoints]\n" + clipboard; 
            var decoder = new LegacyBeatmapDecoder();
            var reader = new LineBufferedReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(corrected)));
            Beatmap beatmap = decoder.Decode(reader);

            var points = beatmap.ControlPointInfo.AllControlPoints.Where(p => p is TimingControlPoint).ToArray();

            if (points.Length == 0)
            {
                showToast("No osu! timing points found in clipboard");
                return;
            }
            
            doPasteDialog(points);
        }
        
        private Beatmap decodeBeatmapFromStream(Stream stream)
        {
            var decoder = new LegacyBeatmapDecoder();
            using var lineBufferedReader = new LineBufferedReader(stream);
            return decoder.Decode(lineBufferedReader);
        }

        private void doPasteDialog(ControlPoint[] points)
        {
            
            dialogOverlay?.Push(new PasteOsuPointsDialog(
                points.Length,
                addAbove: () =>
                {
                    Logger.Log("Pasting osu! timing points above existing ones");
                    foreach (var point in points)
                    {
                        editorBeatmap.ControlPointInfo.Add(point.Time, point);
                    }
                },
                overwrite: () =>
                {
                    Logger.Log("Overwriting existing timing points with osu timing points");
                    editorBeatmap.ControlPointInfo.Clear();
                    foreach (var point in points)
                    {
                        editorBeatmap.ControlPointInfo.Add(point.Time, point);
                    }
                },
                cancel: () =>
                {
                    Logger.Log("Cancelled pasting osu timing points");
                }
            ));
        }

        private void importFromPath(string path)
        {
            
            var screen = this.FindClosestParent<OsuScreen>();

            if (!screen.IsCurrentScreen())
            {
                return;
            }
            
            Logger.Log("Importing timing points from files");
            
            if (path.EndsWith(".zip") || path.EndsWith(".osz"))
            {
                var stream = findEntryStreamInZipOsz(path);
                var beatmap = decodeBeatmapFromStream(stream);
                doPasteDialog(beatmap.ControlPointInfo.AllControlPoints.Where(p => p is TimingControlPoint).ToArray());
            }
            else if (path.EndsWith(".osu") || path.EndsWith(".txt"))
            {
                var beatmap = decodeBeatmapFromStream(File.OpenRead(path));
                doPasteDialog(beatmap.ControlPointInfo.AllControlPoints.Where(p => p is TimingControlPoint).ToArray());
            }
            else
            {
                Logger.Log($"Unsupported file type: {path}");
            }
        }
        
        public Task Import(params string[] paths)
        {
            return Task.Run(() =>
            {
                foreach (var path in paths)
                {
                   importFromPath(path);
                }
            });
        }

        public Task Import(ImportTask[] tasks, ImportParameters parameters = default)
        {
            return Task.Run(() =>
            {
                foreach (var task in tasks)
                {
                    var file = task.Path;
                    importFromPath(file);
                }
            });
        }

        public IEnumerable<string> HandledExtensions { get; } = new[] { ".osu", ".txt", ".zip", ".osz" };

        private Stream findEntryStreamInZipOsz(string zipPath)
        {
            // go through the zip and find the first .osu/.txt file, then return a stream to it
            var archiveReader = new ZipArchiveReader(new FileStream(zipPath, FileMode.Open, FileAccess.Read));
            var entry = archiveReader.Filenames.FirstOrDefault(f => f.EndsWith(".osu") || f.EndsWith(".txt"));
            if (entry == null)
                throw new FileNotFoundException("No .osu or .txt file found in the archive");
            return archiveReader.GetStream(entry);
        }
        
        
        [Resolved]
        private OsuGameBase game { get; set; } = null!;
        
        protected override void LoadComplete()
        {
            base.LoadComplete();

            game.RegisterImportHandler(this);
        }
    }

    public partial class PasteOsuPointsDialog : PopupDialog
    {
        public PasteOsuPointsDialog(int count, Action addAbove, Action overwrite, Action cancel)
        {
            HeaderText = $"Would you like to paste {count} osu! timing points?";

            BodyText =
                "You can choose to either overwrite or merge with your existing timing points. Note that doing this CANNOT BE UNDONE.";
            
            Icon = FontAwesome.Solid.Clipboard;
            
            Buttons = new PopupDialogButton[]
            {
                new PopupDialogDangerousButton
                {
                    Text = @"Overwrite all points!",
                    Action = overwrite
                },
                new PopupDialogDangerousButton()
                {
                    Text = @"Merge with existing points!",
                    Action = addAbove
                },
                new PopupDialogCancelButton
                {
                    Text = "No! Abort mission",
                    Action = cancel
                }
            };
        }
    }
}

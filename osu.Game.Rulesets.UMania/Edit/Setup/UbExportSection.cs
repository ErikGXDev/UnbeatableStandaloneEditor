// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Graphics.Containers;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.OSD;
using osu.Game.Rulesets.UMania.Beatmaps;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Setup;
using WebSocketSharp;
using Container = osu.Framework.Graphics.Containers.Container;
using Logger = osu.Framework.Logging.Logger;

namespace osu.Game.Rulesets.UMania.Edit.Setup
{
    public partial class UbExportSection : SetupSection
    {
        public override LocalisableString Title => "Unbeatable";

        [Resolved] private Editor editor { get; set; } = null!;

        [Resolved] private BeatmapManager beatmapManager { get; set; } = null!;

        [Resolved(canBeNull: true)] private OnScreenDisplay onScreenDisplay { get; set; } = null!;

        [Resolved] private EditorClock editorClock { get; set; } = null!;

        [Resolved] private OsuConfigManager config { get; set; } = null!;
        
        private bool is4Key => config.Get<bool>(OsuSetting.Editor4KeyMode);

    private UbPlaytestButton websocketButton = null!;
        private CancellationTokenSource websocketCheckCancellation = new CancellationTokenSource();

        public void ExportToUnbeatable() => Task.Run(exportToUnbeatable);

        private bool IsWebsocketAvailable()
        {
            try
            {
                using (var ws = new WebSocket("ws://localhost:5080"))
                {
                    ws.Connect();

                    if (ws.ReadyState != WebSocketState.Open)
                        return false;

                    ws.Close();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void StartWebsocketChecks()
        {
            Task.Run(async () =>
            {
                while (!websocketCheckCancellation.IsCancellationRequested)
                {
                    try
                    {
                        bool available = IsWebsocketAvailable();
                        bool practiceFileExists = File.Exists(UbPracticeManager.GetPracticeModeSettingsPath());

                        Schedule(() =>
                        {
                            if (IsDisposed)
                                return;

                            websocketButton.Alpha = available ? 1f : 0f;
                            websocketButton.Enabled.Value = available;
                            websocketButton.SecondButtonVisible = practiceFileExists;
                        });

                        await Task.Delay(TimeSpan.FromSeconds(5), websocketCheckCancellation.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, websocketCheckCancellation.Token);
        }
        
        private async void testAtPracticeTime()
        {
            int startTime = (int)editorClock.CurrentTime;

            string title = Beatmap.Metadata.Title;

            if (string.IsNullOrWhiteSpace(title))
            {
                showToast("Missing song title", "The beatmap needs a title for practice mode to match it.");
                return;
            }

            UbPracticeManager.WritePracticeEntry(title, startTime);

            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                UbPracticeManager.RemovePracticeEntry();
            });

            await Task.Delay(50);

            ExportToUnbeatable();
        }

        private void exportToUnbeatable()
        {
            Logger.Log("Exporting to Unbeatable...");

            var workingBeatmap = editor.Beatmap.Value;

            var beatmapSet = Beatmap.BeatmapInfo.BeatmapSet;

            var difficulty = Beatmap.BeatmapInfo.DifficultyName;

            var beatmaps = getBeatmapsFromSet(beatmapSet);

            // find the correct beatmap in beatmap set by matching difficulty name
            IBeatmap? targetBeatmap = null;

            foreach (var bm in beatmaps)
            {
                if (bm.BeatmapInfo.DifficultyName == difficulty)
                {
                    targetBeatmap = bm;
                    break;
                }
            }

            if (targetBeatmap == null)
            {
                return;
            }

            PassBeatmapConverter passConverter =
                new PassBeatmapConverter(targetBeatmap, targetBeatmap.BeatmapInfo.Ruleset.CreateInstance(), is4Key);

            var playableBeatmap = passConverter.ConvertBeatmap(targetBeatmap, CancellationToken.None);

            UbBeatmapEncoder encoder = new UbBeatmapEncoder(playableBeatmap, null);

            var beatmapStream = new MemoryStream();
            using (var sw = new StreamWriter(beatmapStream, Encoding.UTF8, 1024, leaveOpen: true))
            {
                // Force Windows newlines for exported beatmap files
                sw.NewLine = "\r\n";

                encoder.EncodeB(sw);
            } // StreamWriter is properly disposed here, flushing all content

            // Failsafe: Normalize all newlines to Windows format (\r\n)
            beatmapStream.Seek(0, SeekOrigin.Begin);
            string content;
            using (var reader = new StreamReader(beatmapStream, Encoding.UTF8, true, 1024, leaveOpen: true))
            {
                content = reader.ReadToEnd();
            }

            // Replace all newline variants with Windows newlines
            content = content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");

            // Write normalized content back to stream
            beatmapStream.SetLength(0);
            beatmapStream.Seek(0, SeekOrigin.Begin);
            using (var writer = new StreamWriter(beatmapStream, Encoding.UTF8, 1024, leaveOpen: true))
            {
                writer.Write(content);
            }

            // Audio file
            string audioFilename = Beatmap.Metadata.AudioFile;

            var audioFile = beatmapSet.GetFile(audioFilename);
            if (audioFile == null)
            {
                showToast("Export failed: No audio found", "Audio file not found in beatmap set.");
                return;
            }

            var audioStream = workingBeatmap.GetStream(audioFile.File.GetStoragePath());

            // Temp folder
            string tempPath = Path.Combine(Path.GetTempPath());

            // Save files to temp folder
            string beatmapPath = Path.Combine(tempPath, "temp.osu");

            string websocketPath = beatmapPath;
            
            // On linux, add Z:/ in front to emulate a wine path,
            // which points to the root filesystem
            if (UbPlatform.IsLinux())
            {
                websocketPath = "Z:/" + beatmapPath;
            }

            using (var fs = File.Create(beatmapPath))
            {
                beatmapStream.Seek(0, SeekOrigin.Begin);
                beatmapStream.CopyTo(fs);
            }

            string audioPath = Path.Combine(tempPath, audioFilename);

            using (var fs = File.Create(audioPath))
            {
                audioStream.Seek(0, SeekOrigin.Begin);
                audioStream.CopyTo(fs);
            }

            beatmapStream.Dispose();
            audioStream.Dispose();

            Task.Run(() =>
            {
                using (var ws = new WebSocket("ws://localhost:5080"))
                {
                    ws.Connect();
                    ws.Send("play " + websocketPath);

                    showToast("Export successful", "Sent to Unbeatable!");
                }
            });
        }

        private IBeatmap[] getBeatmapsFromSet(BeatmapSetInfo beatmapSet)
        {
            var beatmaps = new IBeatmap[beatmapSet.Beatmaps.Count];

            for (int i = 0; i < beatmapSet.Beatmaps.Count; i++)
            {
                var beatmapInfo = beatmapSet.Beatmaps[i];
                var beatmap = beatmapManager.GetWorkingBeatmap(beatmapInfo).Beatmap;
                beatmaps[i] = beatmap;
            }

            return beatmaps;
        }

        private MemoryStream getBeatmapStream(IBeatmap beatmap)
        {
            // Export the .osu file
            Logger.Log(beatmap.HitObjects.Count + " hitobjects found.");

            PassBeatmapConverter passConverter =
                new PassBeatmapConverter(beatmap, beatmap.BeatmapInfo.Ruleset.CreateInstance(), is4Key);

            var playableBeatmap = passConverter.ConvertBeatmap(beatmap, CancellationToken.None);

            UbBeatmapEncoder encoder = new UbBeatmapEncoder(playableBeatmap, null);

            var beatmapStream = new MemoryStream();
            using (var sw = new StreamWriter(beatmapStream, Encoding.UTF8, 1024, leaveOpen: true))
            {
                // Force Windows newlines for exported beatmap files
                sw.NewLine = "\r\n";

                encoder.EncodeB(sw);
            } // StreamWriter is properly disposed here, flushing all content

            // Failsafe: Normalize all newlines to Windows format (\r\n)
            beatmapStream.Seek(0, SeekOrigin.Begin);
            string content;
            using (var reader = new StreamReader(beatmapStream, Encoding.UTF8, true, 1024, leaveOpen: true))
            {
                content = reader.ReadToEnd();
            }

            // Replace all newline variants with Windows newlines
            content = content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");

            // Write normalized content back to stream
            beatmapStream.SetLength(0);
            beatmapStream.Seek(0, SeekOrigin.Begin);
            using (var writer = new StreamWriter(beatmapStream, Encoding.UTF8, 1024, leaveOpen: true))
            {
                writer.Write(content);
            }

            return beatmapStream;
        }

        public void ExportToZip(string extension = ".osu") => Task.Run(() => {exportToZip(extension);});
        
        private void exportToZip(string extension = ".osu")
        {

            if (string.IsNullOrEmpty(exportFolderSelector.SelectedDirectory.Value) || Beatmap.BeatmapInfo.BeatmapSet == null)
            {
                showToast("Export failed: Set an export folder", "No export folder selected.");
                return;
            }

            var workingBeatmap = editor.Beatmap.Value;

            var beatmapSet = Beatmap.BeatmapInfo.BeatmapSet;

            string audioFilename = Beatmap.Metadata.AudioFile;

            var audioFile = beatmapSet.GetFile(audioFilename);
            
            string coverFilename = Beatmap.Metadata.BackgroundFile;
            
            var coverFile = beatmapSet.GetFile(coverFilename);

            var baseFilename = "";

            string artist = Beatmap.Metadata.Artist ?? "Unknown";
            string title = Beatmap.Metadata.Title ?? "Song";
            string author = Beatmap.Metadata.Author.Username ?? "Unknown";
            string difficulty = Beatmap.Metadata.Source ?? "Easy";

            if (beatmapSet.Beatmaps.Count > 1)
            {
                baseFilename = $"{artist} - {title} ({author})".GetValidFilename();
            }
            else
            {
                baseFilename = $"{artist} - {title} ({author}) [{difficulty}]".GetValidFilename();
            }

            // Create the .zip file
            string zipFilename = baseFilename + ".zip";

            using (var zipStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {

                    var beatmaps = getBeatmapsFromSet(beatmapSet);

                    foreach (var beatmap in beatmaps)
                    {
                        var stream = getBeatmapStream(beatmap);

                        var newDifficulty = beatmap.Metadata.Source ?? "Easy";

                        var beatmapName = $"{artist} - {title} ({author}) [{newDifficulty}]".GetValidFilename();
                        var beatmapEntry = archive.CreateEntry(beatmapName + extension, CompressionLevel.Optimal);

                        using (var entryStream = beatmapEntry.Open())
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.CopyTo(entryStream);
                        }

                        stream.Dispose();
                    }

                    // Only add audio file if it exists
                    if (audioFile != null)
                    {
                        var audioStream = workingBeatmap.GetStream(audioFile.File.GetStoragePath());
                        if (audioStream != null)
                        {
                            var audioEntry = archive.CreateEntry(audioFilename, CompressionLevel.Optimal);

                            using (var entryStream = audioEntry.Open())
                            {
                                audioStream.Seek(0, SeekOrigin.Begin);
                                audioStream.CopyTo(entryStream);
                            }

                            audioStream.Dispose();
                        }
                    }

                    if (coverFile != null)
                    {
                        var coverStream = workingBeatmap.GetStream(coverFile.File.GetStoragePath());
                        if (coverStream != null)
                        {
                            var coverEntry = archive.CreateEntry("cover.png", CompressionLevel.Optimal);

                            using (var entryStream = coverEntry.Open())
                            {
                                coverStream.Seek(0, SeekOrigin.Begin);
                                coverStream.CopyTo(entryStream);
                            }

                            coverStream.Dispose();
                        }
                    }
                }

                zipStream.Seek(0, SeekOrigin.Begin);

                // Save the .zip file


                // show file save dialog

                var directory = exportFolderSelector.SelectedDirectory.Value;

                var savePath = Path.Combine(directory, zipFilename);



                using (var fs = File.Create(Path.Combine(directory, zipFilename)))
                {
                    zipStream.Seek(0, SeekOrigin.Begin);
                    zipStream.CopyTo(fs);
                }
            }


            Logger.Log($"Exporting to {zipFilename}...");

            showToast("Export successful", $"Saved as {zipFilename}");
        }

        public void ExportToFolder(string extension = ".osu") => Task.Run(() => {exportToFolder(extension);});

        private void exportToFolder(string extension = ".osu")
        {
            if (string.IsNullOrEmpty(exportFolderSelector.SelectedDirectory.Value))
            {
                showToast("Export failed: Set an export folder", "No export folder selected.");
                return;
            }

            var workingBeatmap = editor.Beatmap.Value;

            var beatmapSet = Beatmap.BeatmapInfo.BeatmapSet;

            string audioFilename = Beatmap.Metadata.AudioFile;

            var audioFile = beatmapSet.GetFile(audioFilename);

            var coverFilename = Beatmap.Metadata.BackgroundFile;
            
            var coverFile = beatmapSet.GetFile(coverFilename);


            string artist = Beatmap.Metadata.Artist ?? "Unknown";
            string title = Beatmap.Metadata.Title ?? "Song";
            string author = Beatmap.Metadata.Author.Username ?? "Unknown";
            string difficulty = Beatmap.BeatmapInfo.DifficultyName ?? "Easy";

            var directory = exportFolderSelector.SelectedDirectory.Value;

            var baseFolderName = $"{artist} - {title} ({author})".GetValidFilename();

            directory = Path.Combine(directory, baseFolderName);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var beatmaps = getBeatmapsFromSet(beatmapSet);

            foreach (var beatmap in beatmaps)
            {
                var stream = getBeatmapStream(beatmap);

                var newDifficulty = beatmap.Metadata.Source ?? "Easy";

                var beatmapName = $"{artist} - {title} ({author}) [{newDifficulty}]".GetValidFilename();
                var beatmapPath = Path.Combine(directory, beatmapName + extension);

                using (var fs = File.Create(beatmapPath))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fs);

                }

                stream.Dispose();
            }

            // Only add audio file if it exists
            if (audioFile != null)
            {
                var audioStream = workingBeatmap.GetStream(audioFile.File.GetStoragePath());
                if (audioStream != null)
                {
                    var audioPath = Path.Combine(directory, audioFilename);

                    using (var fs = File.Create(audioPath))
                    {
                        audioStream.Seek(0, SeekOrigin.Begin);
                        audioStream.CopyTo(fs);
                    }

                    audioStream.Dispose();
                }
            }

            if (coverFile != null)
            {
                var coverStream = workingBeatmap.GetStream(coverFile.File.GetStoragePath());
                if (coverStream != null)
                {
                    var coverPath = Path.Combine(directory, "cover.png");

                    using (var fs = File.Create(coverPath))
                    {
                        coverStream.Seek(0, SeekOrigin.Begin);
                        coverStream.CopyTo(fs);
                    }

                    coverStream.Dispose();
                }
            }

            Logger.Log($"Exporting to folder {directory}...");

            showToast("Export successful", $"Saved to folder {baseFolderName}");
        }
        
        public void ExportMap()
        {

            var good = editor.Save();

            if (!good)
            {
                showToast("Export failed: Failed to save", "Failed to save beatmap. Please fix any errors and try again.");
                return;
            }

            showToast("Exporting...", "Please wait...");

            if (exportModeBindable.Value == ExportMode.Folder)
            {
                ExportToFolder();
            }
            else if (exportModeBindable.Value == ExportMode.OfficialFolder)
            {
                ExportToFolder(".txt");
            }
            else if (exportModeBindable.Value == ExportMode.OfficialZip)
            {
                ExportToZip(".txt");
            }
            else if (exportModeBindable.Value == ExportMode.Zip)
            {
                ExportToZip();
            }
        }
        
        public static string GetDataDirectory()
        {
            if (UbPlatform.IsWindows())
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var localLowPath = Path.Combine(userProfile, "AppData", "LocalLow");

                return Path.Combine(localLowPath, "D-CELL GAMES", "UNBEATABLE");
            }

            if (UbPlatform.IsLinux())
            {
                return Path.Combine(GetWinePrefixRoot(), "users", "steamuser", "AppData", "LocalLow", "D-CELL GAMES", "UNBEATABLE");
            }

            // macOS won't have this for now
            return string.Empty;
        }

        public static string GetCustomSongsDirectory()
        {
            return Path.Combine(GetDataDirectory(), "CustomSongs");
        }

        private static string GetWinePrefixRoot()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var steamPath = Path.Combine(userProfile, ".local", "share", "Steam");
            return Path.Combine(steamPath, "steamapps", "compatdata", "2240620", "pfx", "drive_c");
        }
        
        public void OpenGameFolder()
        {
            // Open
            // %USERPROFILE%\AppData\LocalLow\D-CELL GAMES\UNBEATABLE
            // (or the equivalent Proton prefix path on Linux)

            try
            {
                // Resolve LocalLow from the user's profile (reliable on Windows)
                var unbeatablePath = GetDataDirectory();

                if (!Directory.Exists(unbeatablePath))
                {
                    showToast("Error", $"Unbeatable folder not found: {unbeatablePath}");
                    Logger.Log($"Unbeatable folder does not exist: {unbeatablePath}");
                    return;
                }

                string fileName;
                string arguments;

                if (UbPlatform.IsWindows())
                {
                    fileName = "explorer.exe";
                    arguments = '"' + unbeatablePath + '"';
                }
                else if (UbPlatform.IsLinux())
                {
                    fileName = "xdg-open";
                    arguments = '"' + unbeatablePath + '"';
                }
                else
                {
                    showToast("Not supported on macOS yet", "");
                    return;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true
                });

            }
            catch (Exception e)
            {
                Logger.Log($"Failed to open Unbeatable folder: {e.Message}");
                showToast("Error", "Failed to open Unbeatable folder.");
            }
        }

        private partial class BeatmapEditorToast : Toast
        {
            public BeatmapEditorToast(LocalisableString value, string beatmapDisplayName)
                : base(InputSettingsStrings.EditorSection, value)
            {
            }
        }

        private void showToast(string title, string message)
        {
            onScreenDisplay?.Display(new BeatmapEditorToast(title, message));
        }

        private Bindable<ExportMode> exportModeBindable = new Bindable<ExportMode>(ExportMode.OfficialZip);
        private UbExportFolderSelector exportFolderSelector;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            Children = new Drawable[]
            {
                websocketButton = new UbPlaytestButton
                {
                    ExportToUnbeatable = ExportToUnbeatable,
                    TestAtPracticeTime = testAtPracticeTime,
                    Alpha = 0f,
                },
                new FormButton
                {
                    Caption = "Export your beatmap locally for easy sharing",
                    ButtonText = "Export map",
                    Action = ExportMap,
                },
                new FormEnumDropdown<ExportMode>
                {
                    Caption = "Export as",
                    Current = exportModeBindable,
                },
                exportFolderSelector = new UbExportFolderSelector(false, [@".qetiqpuqloekglxmbnmnbfkworitzuokwjfbmvncvmbndf"]) // some extension that is unlikely to be chosen, so only folders are visible
                {
                    Caption = "Export folder",
                    PlaceholderText = "Select folder to export Unbeatable beatmaps to",
                },
                new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 14))
                {
                    Text = "Tip: Select the game's \"CustomSongs\" folder and set \"Export as\" to \"As Folder (.txt)\" to quickly add your custom charts to Unbeatable.",
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Colour = colourProvider.Content1.Opacity(0.7f),
                    Padding = new MarginPadding { Top = 2 },
                },
                new FormButton()
                {
                    Caption = "Open UNBEATABLE Folder",
                    ButtonText = "Open Folder",
                    Action = OpenGameFolder,
                    Alpha = (UbPlatform.IsWindows() || Directory.Exists(GetDataDirectory())) ? 1f : 0f,
                    Margin = new MarginPadding() {Top = 24},
                }

            };


            StartWebsocketChecks();
        }
        
        protected override void Dispose(bool isDisposing)
        {
            websocketCheckCancellation.Cancel();
            websocketCheckCancellation.Dispose();
            base.Dispose(isDisposing);
        }

        enum ExportMode
        {
            [Description("Official Package (.zip file, .txt)")]
            OfficialZip,

            [Description("As Folder (.txt)")]
            OfficialFolder,

            [Description("Legacy Package (.zip file, .osu)")]
            Zip,

            [Description("Legacy Folder (.osu)")]
            Folder,

        }

    }


}

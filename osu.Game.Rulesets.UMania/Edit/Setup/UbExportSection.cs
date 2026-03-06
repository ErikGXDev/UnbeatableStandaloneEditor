// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.IO.Serialization;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.OSD;
using osu.Game.Rulesets.UMania.Beatmaps;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Setup;
using osuTK;
using WebSocketSharp;
using Logger = osu.Framework.Logging.Logger;

namespace osu.Game.Rulesets.UMania.Edit.Setup
{
    public partial class UbExportSection : SetupSection
    {
        public override LocalisableString Title => "Unbeatable";

        [Resolved] private Editor editor { get; set; } = null!;

        [Resolved] private BeatmapManager beatmapManager { get; set; } = null!;

        [Resolved(canBeNull: true)] private OnScreenDisplay onScreenDisplay { get; set; }

        public void ExportToUnbeatable() => Task.Run(exportToUnbeatable);

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
                new PassBeatmapConverter(targetBeatmap, targetBeatmap.BeatmapInfo.Ruleset.CreateInstance());

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
                showToast("Export failed", "Audio file not found in beatmap set.");
                return;
            }

            var audioStream = workingBeatmap.GetStream(audioFile.File.GetStoragePath());

            // Temp folder
            string tempPath = Path.Combine(Path.GetTempPath());

            // Save files to temp folder
            string beatmapPath = Path.Combine(tempPath, "temp.osu");

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
                    ws.Send("play " + beatmapPath);

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
                new PassBeatmapConverter(beatmap, beatmap.BeatmapInfo.Ruleset.CreateInstance());

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

            if (string.IsNullOrEmpty(exportFolderSelector.SelectedDirectory.Value))
            {
                showToast("Export failed", "No export folder selected.");
                return;
            }

            var workingBeatmap = editor.Beatmap.Value;

            var beatmapSet = Beatmap.BeatmapInfo.BeatmapSet;

            string audioFilename = Beatmap.Metadata.AudioFile;

            var audioFile = beatmapSet.GetFile(audioFilename);

            var baseFilename = "";

            string artist = Beatmap.Metadata.Artist ?? "Unknown";
            string title = Beatmap.Metadata.Title ?? "Song";
            string author = Beatmap.Metadata.Author.Username ?? "Unknown";
            string difficulty = Beatmap.BeatmapInfo.DifficultyName ?? "Easy";

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

                        var newDifficulty = beatmap.BeatmapInfo.DifficultyName ?? "Easy";

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
                showToast("Export failed", "No export folder selected.");
                return;
            }

            var workingBeatmap = editor.Beatmap.Value;

            var beatmapSet = Beatmap.BeatmapInfo.BeatmapSet;

            string audioFilename = Beatmap.Metadata.AudioFile;

            var audioFile = beatmapSet.GetFile(audioFilename);


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

                var newDifficulty = beatmap.BeatmapInfo.DifficultyName ?? "Easy";

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

            Logger.Log($"Exporting to folder {directory}...");

            showToast("Export successful", $"Saved to folder {baseFolderName}");
        }


        public void ExportMap()
        {

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


        public bool IsWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }
        public void OpenGameFolder()
        {
            // Open
            // %USERPROFILE%\AppData\LocalLow\D-CELL GAMES\UNBEATABLE

            if (!IsWindows())
            {
                showToast("Error", "Opening Unbeatable folder is only supported on Windows.");
                return;
            }

            try
            {
                // Resolve LocalLow from the user's profile (reliable on Windows)
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var localLowPath = Path.Combine(userProfile, "AppData", "LocalLow");

                var unbeatablePath = Path.Combine(localLowPath, "D-CELL GAMES", "UNBEATABLE");

                if (!Directory.Exists(unbeatablePath))
                {
                    showToast("Error", $"Unbeatable folder not found: {unbeatablePath}");
                    Logger.Log($"Unbeatable folder does not exist: {unbeatablePath}");
                    return;
                }

                // Use explorer.exe to open the folder (works reliably for directories)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = '"' + unbeatablePath + '"',
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
        private void load()
        {
            Children = new Drawable[]
            {
                new FormButton
                {
                    Caption = "Test your map in Unbeatable (Through Websocket)",
                    ButtonText = "Test Beatmap",
                    Action = ExportToUnbeatable,
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
                new FormButton()
                {
                    Caption = "Open UNBEATABLE Folder",
                    ButtonText = "Open Folder",
                    Action = OpenGameFolder,
                    Alpha = IsWindows() ? 1f : 0f,
                    Margin = new MarginPadding() {Top = 24},
                }

            };
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

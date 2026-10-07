// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using osu.Game.Rulesets.UMania.FMOD;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Custom;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Cursor;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.OSD;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.UMania.Beatmaps;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Setup;
using osu.Game.Screens.Edit.Verify;
using osuTK.Graphics;
using WebSocketSharp;
using Container = osu.Framework.Graphics.Containers.Container;
using Logger = osu.Framework.Logging.Logger;

namespace osu.Game.Rulesets.UMania.Edit.Setup
{
    public partial class UbExportSection : SetupSection, IExportsUnbeatable
    {
        public override LocalisableString Title => "Exporting";

        [Resolved(CanBeNull = true)] private SetupScreen setupScreen { get; set; } = null!;
        
        [Resolved] private Editor editor { get; set; } = null!;

        [Resolved] private BeatmapManager beatmapManager { get; set; } = null!;

        [Resolved(canBeNull: true)] private OnScreenDisplay onScreenDisplay { get; set; } = null!;

        [Resolved] private EditorClock editorClock { get; set; } = null!;

        [Resolved] private OsuConfigManager config { get; set; } = null!;

        [Resolved] private GameHost gameHost { get; set; } = null!;
        
        private Color4 accentColour = Color4.White;

        private bool is4Key => config.Get<bool>(OsuSetting.Editor4KeyMode);

        private int msOffset => config.Get<int>(OsuSetting.EditorExportOffsetMs);

        private UbPlaytestButton websocketButton = null!;
        private CancellationTokenSource websocketCheckCancellation = new CancellationTokenSource();

        public void ExportToUnbeatable()
        {
            var good = editor.Save();
            
            if (!good)
            {
                showToast("Export failed: Failed to save",
                    "Failed to save beatmap. Please fix any errors and try again.");
                return;
            }
            
            Task.Run(exportToUnbeatable);
        }

        public bool IsWebsocketAvailable()
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

        public async void TestAtPracticeTime()
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


        private string getBaseFilename(string artist, string title, string author)
        {
            if (string.IsNullOrWhiteSpace(artist))
                artist = "Unknown";

            if (string.IsNullOrWhiteSpace(title))
                title = "Song";

            if (string.IsNullOrWhiteSpace(author))
                author = "Unknown";
            
            if (config.Get<bool>(OsuSetting.EditorShortNames))
            {
                author = author.Truncate(20, "...");
            }

            return $"{artist} - {title} ({author})".GetValidFilename();
        }
        
        private string getBaseFilenameWithDiff(string artist, string title, string author, string difficulty)
        {
            if (string.IsNullOrWhiteSpace(artist))
                artist = "Unknown";

            if (string.IsNullOrWhiteSpace(title))
                title = "Song";

            if (string.IsNullOrWhiteSpace(author))
                author = "Unknown";

            if (string.IsNullOrWhiteSpace(difficulty))
                difficulty = "Easy";

            if (config.Get<bool>(OsuSetting.EditorShortNames))
            {
                return $"[{difficulty}]".GetValidFilename();
            }

            return $"{artist} - {title} ({author}) [{difficulty}]".GetValidFilename();
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
                new PassBeatmapConverter(targetBeatmap, targetBeatmap.BeatmapInfo.Ruleset.CreateInstance(), is4Key,
                    msOffset);

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
                new PassBeatmapConverter(beatmap, beatmap.BeatmapInfo.Ruleset.CreateInstance(), is4Key, msOffset);

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

        public void ExportToZip(string extension = ".osu", bool insideFolder = false) => Task.Run(() => { exportToZip(extension, insideFolder); });

        
        private static string getZipEntryName(string prefix, string name)
        {
            name = name.Replace('\\', '/');

            return string.IsNullOrEmpty(prefix) ? name : $"{prefix}/{name}";
        }

        private void exportToZip(string extension = ".osu", bool insideFolder = false)
        {
            if (string.IsNullOrEmpty(exportFolderSelector.SelectedDirectory.Value) ||
                Beatmap.BeatmapInfo.BeatmapSet == null)
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

            string videoFilename = workingBeatmap.Storyboard.PrimaryVideo?.Path ?? string.Empty;
            var videoFile = string.IsNullOrEmpty(videoFilename) ? null : beatmapSet.GetFile(videoFilename);

            var baseFilename = "";

            string artist = Beatmap.Metadata.Artist ?? "Unknown";
            string title = Beatmap.Metadata.Title ?? "Song";
            string author = Beatmap.Metadata.Author.Username ?? "Unknown";
            
            baseFilename = getBaseFilename(artist, title, author);
            
            // Create the .zip file
            string zipFilename = baseFilename + ".zip";
            
            string entryPrefix = insideFolder ? baseFilename : string.Empty;

            var directory = exportFolderSelector.SelectedDirectory.Value;

            var savePath = Path.Combine(directory, zipFilename);

            using (var zipStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    var beatmaps = getBeatmapsFromSet(beatmapSet);

                    foreach (var beatmap in beatmaps)
                    {
                        var stream = getBeatmapStream(beatmap);

                        var newDifficulty = beatmap.Metadata.Source ?? "Easy";

                        var beatmapName = getBaseFilenameWithDiff(artist, title, author, newDifficulty);
                        var beatmapEntry = archive.CreateEntry(getZipEntryName(entryPrefix, beatmapName + extension), CompressionLevel.Optimal);

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
                            var audioEntry = archive.CreateEntry(getZipEntryName(entryPrefix, audioFilename), CompressionLevel.Optimal);

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
                            var coverEntry = archive.CreateEntry(getZipEntryName(entryPrefix, "cover.png"), CompressionLevel.Optimal);

                            using (var entryStream = coverEntry.Open())
                            {
                                coverStream.Seek(0, SeekOrigin.Begin);
                                coverStream.CopyTo(entryStream);
                            }

                            coverStream.Dispose();
                        }
                    }

                    if (videoFile != null)
                    {
                        var videoStream = workingBeatmap.GetStream(videoFile.File.GetStoragePath());
                        if (videoStream != null)
                        {
                            string videoEntryName = videoFilename.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
                                ? "video.webm"
                                : "video.mp4";
                            var videoEntry = archive.CreateEntry(getZipEntryName(entryPrefix, videoEntryName), CompressionLevel.Optimal);

                            using (var entryStream = videoEntry.Open())
                            {
                                videoStream.Seek(0, SeekOrigin.Begin);
                                videoStream.CopyTo(entryStream);
                            }

                            videoStream.Dispose();
                        }
                    }
                }

                zipStream.Seek(0, SeekOrigin.Begin);

                // Save the .zip file


                // show file save dialog

                using (var fs = File.Create(savePath))
                {
                    zipStream.Seek(0, SeekOrigin.Begin);
                    zipStream.CopyTo(fs);
                }
            }


            Logger.Log($"Exporting to {zipFilename}...");

            showExportToast("Export successful", $"Saved as {zipFilename}", savePath);
        }

        public void ExportToFolder(string extension = ".osu") => Task.Run(() => { exportToFolder(extension); });

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

            string videoFilename = workingBeatmap.Storyboard.PrimaryVideo?.Path ?? string.Empty;
            var videoFile = string.IsNullOrEmpty(videoFilename) ? null : beatmapSet.GetFile(videoFilename);

            string artist = Beatmap.Metadata.Artist ?? "Unknown";
            string title = Beatmap.Metadata.Title ?? "Song";
            string author = Beatmap.Metadata.Author.Username ?? "Unknown";

            var directory = exportFolderSelector.SelectedDirectory.Value;

            var baseFolderName = getBaseFilename(artist, title, author);

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

                var beatmapName = getBaseFilenameWithDiff(artist, title, author, newDifficulty);
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

            if (videoFile != null)
            {
                var videoStream = workingBeatmap.GetStream(videoFile.File.GetStoragePath());
                if (videoStream != null)
                {
                    string videoEntryName = videoFilename.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
                        ? "video.webm"
                        : "video.mp4";
                    var videoPath = Path.Combine(directory, videoEntryName);

                    using (var fs = File.Create(videoPath))
                    {
                        videoStream.Seek(0, SeekOrigin.Begin);
                        videoStream.CopyTo(fs);
                    }

                    videoStream.Dispose();
                }
            }

            Logger.Log($"Exporting to folder {directory}...");

            showExportToast("Export successful", $"Saved to folder {baseFolderName}", directory);
        }

        public void ExportMap()
        {
            var good = editor.Save();

            if (!good)
            {
                showToast("Export failed: Failed to save",
                    "Failed to save beatmap. Please fix any errors and try again.");
                return;
            }

            showToast("Exporting...", "Please wait...");

            //warningText.FlashColour(Color4.LightYellow, 500);
            

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
            else if (exportModeBindable.Value == ExportMode.FolderInPackage)
            {
                ExportToZip(".txt", insideFolder: true);
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
                return Path.Combine(GetWinePrefixRoot(), "users", "steamuser", "AppData", "LocalLow", "D-CELL GAMES",
                    "UNBEATABLE");
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
        
        private partial class UbExportSuccessToast : Toast
        {
            public UbExportSuccessToast(LocalisableString description, LocalisableString value, string revealPath,
                                         string revealText, Action<string> revealAction, Color4 accentColour)
                : base(description, value)
            {
                if (string.IsNullOrEmpty(revealPath))
                    return;

                ValueSpriteText.Y = -5;

                var revealButton = new UbRevealButton
                {
                    Text = revealText,
                    BackgroundColour = accentColour,
                    RelativeSizeAxes = Axes.X,
                    Width = 0.42f * 1.05f,
                    Height = 30 * 1.05f,
                    Scale = new Vector2(0.9f),
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    HasTriangles = false,
                    Margin = new MarginPadding { Bottom = 10, Top = 20 },
                    Action = () => revealAction?.Invoke(revealPath),
                };

                Content.Add(revealButton);
            }
        }
        
        private partial class UbRevealButton : RoundedButton
        {
            public UbRevealButton()
            {
                Add(new SpriteIcon
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    X = 10,
                    Size = new Vector2(13),
                    Icon = FontAwesome.Solid.FolderOpen,
                    Depth = -1,
                });

                SpriteText.Margin = new MarginPadding { Left = 14 };
            }
        }
        
        private void revealExportedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                gameHost.PresentFileExternally(path);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to reveal exported path: {e.Message}");
            }
        }

        private void showToast(string title, string message)
        {
            onScreenDisplay?.Display(new BeatmapEditorToast(title, message));
        }

        private void approximateExportOffset()
        {
            string audioPath;

            try
            {
                audioPath = resolveAudioFilePath();
            }
            catch (Exception e)
            {
                showToast("Could not approximate offset", e.Message);
                return;
            }

            approximateOffsetButton.Enabled.Value = false;
            
            Task.Run(async () =>
            {
                try
                {
                    var result = await FmodOffsetAnalyzer.AnalyseAsync(audioPath);

                    Schedule(() =>
                    {
                        if (!result.Success)
                        {
                            Logger.Log("Offset approximation failed: " + result.Summary);
                            showToast("Could not approximate offset", result.Summary);
                            return;
                        }

                        int delta = (int)Math.Round(result.OffsetMs);
                        
                        int visualBias = (int)Math.Round(Editor.WAVEFORM_VISUAL_OFFSET);

                        int gameBias = -60;

                        config.GetBindable<int>(OsuSetting.EditorExportOffsetMs).Value = Math.Clamp(visualBias + delta + gameBias, -1000, 1000);

                        // note: message does not actually work and is hidden.
                        showToast("Offset approximated", $"{result.Summary}\n\nExport offset set to {visualBias + delta}ms (including {visualBias}ms editor waveform bias).");
                    });
                }
                catch (Exception e)
                {
                    Schedule(() => showToast("Could not approximate offset", $"The audio could not be compared with FMOD:\n{e.Message}"));
                }
                finally
                {
                    Schedule(() => approximateOffsetButton.Enabled.Value = true);
                }
            });
        }

        private string resolveAudioFilePath()
        {
            var beatmapSet = Beatmap.BeatmapInfo.BeatmapSet;

            string audioFilename = Beatmap.Metadata.AudioFile;

            var audioFile = beatmapSet.GetFile(audioFilename);
            if (audioFile == null)
                throw new FileNotFoundException($"Audio file \"{audioFilename}\" not found in the beatmap set.");

            return gameHost.Storage.GetFullPath(Path.Combine(@"files", audioFile.File.GetStoragePath()));
        }
        
        private void showExportToast(string title, string message, string revealPath, string revealText = "Open in explorer")
        {
            onScreenDisplay?.Display(new UbExportSuccessToast(title, message, revealPath, revealText, revealExportedPath, accentColour));
        }

        private Bindable<ExportMode> exportModeBindable = new Bindable<ExportMode>(ExportMode.OfficialZip);
        private UbExportFolderSelector exportFolderSelector;
        private OsuTextFlowContainer warningText;
        private IssueList issueList;
        private ApproximateOffsetButton approximateOffsetButton;

        private partial class ApproximateOffsetButton : RoundedButton, IHasCustomTooltip
        {
            public ApproximateOffsetButton() { }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Content.CornerRadius = 6;
            }

            public ITooltip GetCustomTooltip() => new ApproxTooltip();

            public object? TooltipContent => TooltipText;
        }
        
        private partial class ApproxTooltip : OsuTooltipContainer.OsuTooltip
        {
            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                CornerRadius = 5;
                Masking = true;
                
                var textFlowContainer = Content.ChildrenOfType<TextFlowContainer>().FirstOrDefault();
                if (textFlowContainer != null)
                {
                    textFlowContainer.MaximumSize = new Vector2(420f, float.PositiveInfinity);
                }
            }

            public override void Move(Vector2 pos)
            {
               
                // This method is called every frame so we can do this safely here.
                Position = Interpolation.ValueAt(Time.Elapsed, Position, pos + new Vector2(-30, 0), 0, 120, Easing.OutQuint);
            }
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, OsuColour colours)
        {
            accentColour = colourProvider.Background2.Darken(0.8f);

            Children = new Drawable[]
            {
                websocketButton = new UbPlaytestButton
                {
                    ExportToUnbeatable = ExportToUnbeatable,
                    TestAtPracticeTime = TestAtPracticeTime,
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
                exportFolderSelector =
                    new UbExportFolderSelector(false,
                        [
                            @".qetiqpuqloekglxmbnmnbfkworitzuokwjfbmvncvmbndf"
                        ]) // some extension that is unlikely to be chosen, so only folders are visible
                        {
                            Caption = "Export folder",
                            PlaceholderText = "Select folder to export Unbeatable beatmaps to",
                        },
                new Container()
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Masking = true,
                    CornerRadius = 4,
                    Children = new Drawable[]
                    {
                        new FormControlBackground(),
                        new TooltipNumberInput
                        {
                            Width = 0.815f,
                            Padding = new MarginPadding() {Left = 8, Right = 0, Vertical = 6},
                            LabelText = "Export note offset (ms)",
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            TooltipText = TooltipNumberInput.OffsetTooltipShort,
                            Current = config.GetBindable<int>(OsuSetting.EditorExportOffsetMs),
                            MinimumValue = -1000,
                            MaximumValue = 1000,
                        },
                        approximateOffsetButton = new ApproximateOffsetButton()
                        {
                            Text = "Detect",
                            TooltipText = "Offset detection works best with a -60ms chart offset in-game and a chart aligned to the timeline waveform in the editor.",
                            Action = approximateExportOffset,
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            HasTriangles = false,
                            BackgroundColour = colourProvider.Colour4,
                            Width = 80f,
                            Margin = new MarginPadding(6) { Left = 0, Right = 6, Vertical = 6 },
                            Scale = new Vector2(0.9f),
                            Height = 26,
                        }
                    },
                },
                /*new Container()
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Masking = true,
                    CornerRadius = 4,
                    Children = new Drawable[]
                    {
                        new FormControlBackground(),
                        approximateOffsetButton = new FormButton()
                        {
                            Caption = "Approximate offset between BASS and FMOD",
                            ButtonText = "Detect",
                            Action = approximateExportOffset,
                        }
                    },
                },*/
                
                warningText = new OsuTextFlowContainer(t => t.Font = t.Font.With(size: 14))
                {
                    Text = "",
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Colour = colourProvider.Content1.Opacity(0.7f),
                    Alpha = 0f,
                    Padding = new MarginPadding { Top = 2 },
                },
                new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 14))
                {
                    Text =
                        "Tip: Select the game's \"CustomSongs\" folder and set \"Export as\" to \"As Folder (.txt)\" to quickly add your custom charts to Unbeatable.",
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
                    Margin = new MarginPadding() { Top = 24 },
                }
            };
            
            StartWebsocketChecks();
        }
        
        [Resolved]
        private IBindable<WorkingBeatmap> workingBeatmap { get; set; }

        [Resolved]
        private EditorBeatmap beatmap { get; set; }

        private void checkIssues()
        {
            var generalVerifier = new BeatmapVerifier();
            var rulesetVerifier = beatmap.BeatmapInfo.Ruleset.CreateInstance().CreateBeatmapVerifier();

            var context = BeatmapVerifierContext.Create(
                beatmap,
                workingBeatmap.Value,
                DifficultyRating.Hard,
                beatmapManager
            );
            
            var issues = generalVerifier.Run(context);

            if (rulesetVerifier != null)
                issues = issues.Concat(rulesetVerifier.Run(context));

            var issuesList = issues.ToList();

            var importantIssueCount = issuesList.Count(issue => issue.Template.Type == IssueType.Problem);
            
            if (importantIssueCount > 0)
            {
                
                var text = "You may have issues in your chart. Check the Verify tab for details.";
                
                /*var text = "Warning! You have " + importantIssueCount + " important issue";
                
                if (issuesList.Count > 1)
                {
                    text += "s";
                }
                
                text += " in your beatmap. Please check the Verify tab for details.";*/
                
                warningText.FadeIn(200);
                warningText.Text = text;
            }
            else
            {
                warningText.Alpha = 0f;
            }
        }
        
        protected override void Update()
        {
            if (setupScreen != null && setupScreen.UpdatedTime == -1) // Check every 5 seconds
            {
                Logger.Log("Checking for issues in beatmap...");
                checkIssues();
                setupScreen.UpdatedTime = Time.Current;
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var exportFolderConfig = config.GetBindable<string>(OsuSetting.EditorExportFolder);

            if (!string.IsNullOrEmpty(exportFolderConfig.Value))
            {
                // Hacky, but works
                Schedule(() =>
                {
                    Schedule(() => { exportFolderSelector.SelectedDirectory.Value = exportFolderConfig.Value; });
                });
            }

            Logger.Log($"Export folder set to: {exportFolderSelector.SelectedDirectory.Value}");

            exportFolderSelector.SelectedDirectory.BindValueChanged(_ =>
            {
                Logger.Log("Export folder changed to: " + exportFolderSelector.SelectedDirectory.Value);
                config.SetValue(OsuSetting.EditorExportFolder, exportFolderSelector.SelectedDirectory.Value);
            });

            var exportModeConfig = config.GetBindable<int>(OsuSetting.EditorExportMode);

            Logger.Log($"Export mode set to: {exportModeConfig.Value}");

            if (Enum.IsDefined(typeof(ExportMode), exportModeConfig.Value))
            {
                exportModeBindable.Value = (ExportMode)exportModeConfig.Value;
            }
            else
            {
                exportModeBindable.Value = ExportMode.OfficialZip;
            }

            exportModeBindable.BindValueChanged(_ =>
            {
                config.SetValue(OsuSetting.EditorExportMode, (int)exportModeBindable.Value);
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            websocketCheckCancellation.Cancel();
            websocketCheckCancellation.Dispose();
            base.Dispose(isDisposing);
        }

        [HasOrderedElements]
        enum ExportMode
        {
            [Description("Official Package (.zip file, .txt)")]
            [Order(0)]
            OfficialZip = 0,
            
            [Description("Package with Folder (.zip file, .txt)")]
            [Order(2)]
            FolderInPackage = 4,

            [Description("As Folder (.txt)")]
            [Order(1)]
            OfficialFolder = 1,

            [Description("Legacy Package (.zip file, .osu)")]
            [Order(3)]
            Zip = 2,

            [Description("Legacy Folder (.osu)")]
            [Order(4)]
            Folder = 3,
        }
    }
}
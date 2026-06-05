// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Models;
using osu.Game.Overlays;
using osu.Game.Screens.Backgrounds;
using osu.Game.Screens.Edit.Components;
using osu.Game.Skinning;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Setup
{
    public partial class ResourcesSection : SetupSection
    {
        private FormBeatmapFileSelector audioTrackChooser = null!;
        private FormBeatmapFileSelector backgroundChooser = null!;
        private FormBeatmapFileSelector hitsoundChooser = null!;
        private HitsoundDeleteButton hitsoundDeleteButton = null!;

        private readonly Bindable<EditorBeatmapSkin.SampleSet?> currentSampleSet = new Bindable<EditorBeatmapSkin.SampleSet?>();

        public override LocalisableString Title => EditorSetupStrings.ResourcesHeader;

        [Resolved]
        private MusicController music { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> working { get; set; } = null!;

        [Resolved]
        private Editor? editor { get; set; }

        [Resolved]
        private SetupScreen setupScreen { get; set; } = null!;
        
        [Resolved]
        private Storage storage { get; set; } = null!;

        private FormTextBox coverArtist = null!;

        private SetupScreenHeaderBackground headerBackground = null!;
        
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;
        

        [BackgroundDependencyLoader]
        private void load()
        {
            headerBackground = new SetupScreenHeaderBackground
            {
                RelativeSizeAxes = Axes.X,
                Height = 110,
            };

            bool beatmapHasMultipleDifficulties = working.Value.BeatmapSetInfo.Beatmaps.Count > 1;

            Children = new Drawable[]
            {
                audioTrackChooser = new FormBeatmapFileSelector(beatmapHasMultipleDifficulties, SupportedExtensions.AUDIO_EXTENSIONS)
                {
                    Caption = EditorSetupStrings.AudioTrack,
                    PlaceholderText = EditorSetupStrings.ClickToSelectTrack,
                },
                backgroundChooser = new FormBeatmapFileSelector(beatmapHasMultipleDifficulties, SupportedExtensions.IMAGE_EXTENSIONS)
                {
                    Caption = "Cover Art",
                    PlaceholderText = EditorSetupStrings.ClickToSelectBackground,
                },
                coverArtist = new FormTextBox()
                {
                    Caption = "Cover Artist",
                    Current = MetadataSection.coverArtistBindable,
                },
                hitsoundChooser = new FormBeatmapFileSelector(beatmapHasMultipleDifficulties, SupportedExtensions.AUDIO_EXTENSIONS)
                {
                    Caption = "Custom Hitsound (global)",
                    PlaceholderText = "Click to add a hitsound",
                    Margin = new MarginPadding { Top = 8 }
                },
                hitsoundDeleteButton = new HitsoundDeleteButton()
               
                // FIX: Hide sample set chooser because they're not needed
                /*new FormSampleSetChooser
                {
                    Alpha = 0,
                    Current = { BindTarget = currentSampleSet },
                },
                new FormSampleSet
                {
                    Alpha = 0,
                    Current = { BindTarget = currentSampleSet },
                    SampleAddRequested = (file, targetName) =>
                    {
                        string actualFilename = string.Concat(targetName, file.Extension);
                        using var stream = file.OpenRead();
                        beatmaps.AddFile(working.Value.BeatmapSetInfo, stream, actualFilename);
                        return actualFilename;
                    },
                    SampleRemoveRequested = filename =>
                    {
                        var file = working.Value.BeatmapSetInfo.GetFile(filename);
                        if (file != null)
                            beatmaps.DeleteFile(working.Value.BeatmapSetInfo, file);
                    }
                },*/
            };
            
            

            backgroundChooser.PreviewContainer.Add(headerBackground);

            if (!string.IsNullOrEmpty(working.Value.Metadata.BackgroundFile))
                backgroundChooser.Current.Value = new FileInfo(working.Value.Metadata.BackgroundFile);

            if (!string.IsNullOrEmpty(working.Value.Metadata.AudioFile))
                audioTrackChooser.Current.Value = new FileInfo(working.Value.Metadata.AudioFile);
            
            Schedule(hitsoundDisplayUpdate);

            backgroundChooser.Current.BindValueChanged(backgroundChanged);
            audioTrackChooser.Current.BindValueChanged(audioTrackChanged);
            hitsoundChooser.Current.BindValueChanged(hitsoundChanged);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Schedule(hitsoundDisplayUpdate);
        }


        public bool ChangeBackgroundImage(FileInfo source, bool applyToAllDifficulties)
        {
            if (!source.Exists)
                return false;

            changeResource(source, applyToAllDifficulties, @"bg",
                metadata => metadata.BackgroundFile,
                (metadata, name) => metadata.BackgroundFile = name);

            headerBackground.UpdateBackground();
            editor?.ApplyToBackground(bg => ((EditorBackgroundScreen)bg).RefreshBackground());
            return true;
        }

        public bool ChangeAudioTrack(FileInfo source, bool applyToAllDifficulties)
        {
            if (!source.Exists)
                return false;

            string artist;
            string title;

            try
            {
                using (var tagSource = TagLibUtils.GetTagLibFile(source.FullName))
                {
                    artist = tagSource.Tag.JoinedAlbumArtists ?? tagSource.Tag.JoinedPerformers;
                    title = tagSource.Tag.Title;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "The selected audio track appears to be corrupted. Please select another one.");
                return false;
            }

            changeResource(source, applyToAllDifficulties, @"audio",
                metadata => metadata.AudioFile,
                (metadata, name) =>
                {
                    metadata.AudioFile = name;

                    if (!string.IsNullOrWhiteSpace(artist))
                    {
                        metadata.ArtistUnicode = artist;
                        metadata.Artist = MetadataUtils.StripNonRomanisedCharacters(metadata.ArtistUnicode);
                    }

                    if (!string.IsNullOrEmpty(title))
                    {
                        metadata.TitleUnicode = title;
                        metadata.Title = MetadataUtils.StripNonRomanisedCharacters(metadata.TitleUnicode);
                    }
                });

            music.ReloadCurrentTrack();
            setupScreen.MetadataChanged?.Invoke();
            return true;
        }

        private void changeResource(FileInfo source, bool applyToAllDifficulties, string baseFilename, Func<BeatmapMetadata, string> readFilename, Action<BeatmapMetadata, string> writeMetadata)
        {
            var set = working.Value.BeatmapSetInfo;
            var beatmap = working.Value.BeatmapInfo;

            var otherBeatmaps = set.Beatmaps.Where(b => !b.Equals(beatmap));

            // First, clean up files which will no longer be used.
            if (applyToAllDifficulties)
            {
                foreach (var b in set.Beatmaps)
                {
                    if (set.GetFile(readFilename(b.Metadata)) is RealmNamedFileUsage otherExistingFile)
                        beatmaps.DeleteFile(set, otherExistingFile);
                }
            }
            else
            {
                RealmNamedFileUsage? oldFile = set.GetFile(readFilename(working.Value.Metadata));

                if (oldFile != null)
                {
                    bool oldFileUsedInOtherDiff = otherBeatmaps
                        .Any(b => readFilename(b.Metadata) == oldFile.Filename);
                    if (!oldFileUsedInOtherDiff)
                        beatmaps.DeleteFile(set, oldFile);
                }
            }

            // Choose a new filename that doesn't clash with any other existing files.
            string newFilename = $"{baseFilename}{source.Extension}";

            if (set.GetFile(newFilename) != null)
            {
                string[] existingFilenames = set.Files.Select(f => f.Filename).Where(f =>
                    f.StartsWith(baseFilename, StringComparison.OrdinalIgnoreCase) &&
                    f.EndsWith(source.Extension, StringComparison.OrdinalIgnoreCase)).ToArray();
                newFilename = NamingUtils.GetNextBestFilename(existingFilenames, $@"{baseFilename}{source.Extension}");
            }

            using (var stream = source.OpenRead())
                beatmaps.AddFile(set, stream, newFilename);

            if (applyToAllDifficulties)
            {
                foreach (var b in otherBeatmaps)
                {
                    writeMetadata(b.Metadata, newFilename);

                    // save the difficulty to re-encode the .osu file, updating any reference of the old filename.
                    //
                    // note that this triggers a full save flow, including triggering a difficulty calculation.
                    // this is not a cheap operation and should be reconsidered in the future.
                    var beatmapWorking = beatmaps.GetWorkingBeatmap(b);
                    beatmaps.Save(b, beatmapWorking.GetPlayableBeatmap(b.Ruleset), beatmapWorking.GetSkin());
                }
            }

            writeMetadata(beatmap.Metadata, newFilename);

            // editor change handler cannot be aware of any file changes or other difficulties having their metadata modified.
            // for simplicity's sake, trigger a save when changing any resource to ensure the change is correctly saved.
            editor?.Save();
        }

        // to avoid scaring users, both background & audio choosers use fake `FileInfo`s with user-friendly filenames
        // when displaying an imported beatmap rather than the actual SHA-named file in storage.
        // however, that means that when a background or audio file is chosen that is broken or doesn't exist on disk when switching away from the fake files,
        // the rollback could enter an infinite loop, because the fake `FileInfo`s *also* don't exist on disk - at least not in the fake location they indicate.
        // to circumvent this issue, just allow rollback to proceed always without actually running any of the change logic to ensure visual consistency.
        // note that this means that `Change{BackgroundImage,AudioTrack}()` are required to not have made any modifications to the beatmap files
        // (or at least cleaned them up properly themselves) if they return `false`.
        private bool rollingBackBackgroundChange;
        private bool rollingBackAudioChange;

        private void backgroundChanged(ValueChangedEvent<FileInfo?> file)
        {
            if (rollingBackBackgroundChange)
                return;

            if (file.NewValue == null || !ChangeBackgroundImage(file.NewValue, backgroundChooser.ApplyToAllDifficulties.Value))
            {
                rollingBackBackgroundChange = true;
                backgroundChooser.Current.Value = file.OldValue;
                rollingBackBackgroundChange = false;
            }
        }

        private void audioTrackChanged(ValueChangedEvent<FileInfo?> file)
        {
            if (rollingBackAudioChange)
                return;

            if (file.NewValue == null || !ChangeAudioTrack(file.NewValue, audioTrackChooser.ApplyToAllDifficulties.Value))
            {
                rollingBackAudioChange = true;
                audioTrackChooser.Current.Value = file.OldValue;
                rollingBackAudioChange = false;
            }
        }

        [Resolved] private SkinManager skinManager { get; set; } = null!;

        private void hitsoundChanged(ValueChangedEvent<FileInfo?> file)
        {
            if (file.NewValue == null)
                return;
            
            // Delete previous files
            foreach (var exts in SupportedExtensions.AUDIO_EXTENSIONS)
            {
                if (storage.Exists("custom/hitsound" + exts))
                {
                    storage.Delete("custom/hitsound" + exts);
                }
            }

            var stream = storage.CreateFileSafely("custom/hitsound" + file.NewValue.Extension);

            using (var sourceStream = file.NewValue.OpenRead())
            {
                sourceStream.CopyTo(stream);
            }
            
            stream.Close();

            Schedule(() =>
            {
                skinManager.TriggerSourceChanged();
                Logger.Log("Requested new skin.");
                hitsoundDisplayUpdate();

            });
        }

        private void hitsoundDisplayUpdate()
        {
            skinManager.TriggerSourceChanged();

            // Enable/Disable hitsound delete button based on whether a custom hitsound exists or not
            // (as well as update hitsoundChooser.filenameText.Text)
            var exists = false;
            foreach (var ext in SupportedExtensions.AUDIO_EXTENSIONS)
            {
                if (storage.Exists("custom/hitsound" + ext))
                {
                    exists = true;
                }
            }

            if (exists)
            {
                hitsoundDeleteButton.Enabled.Value = true;
                hitsoundDeleteButton.Action = () =>
                {
                    foreach (var ext in SupportedExtensions.AUDIO_EXTENSIONS)
                    {
                        if (storage.Exists("custom/hitsound" + ext))
                        {
                            storage.Delete("custom/hitsound" + ext);
                        }
                    }

                    Schedule(() =>
                    {
                        skinManager.TriggerSourceChanged();
                        Logger.Log("Deleted custom hitsound and requested new skin.");
                        hitsoundDisplayUpdate();
                    });

                };

                Schedule(() => { 
                    hitsoundChooser.filenameText.Text = "custom/hitsound.mp3";
                    hitsoundChooser.filenameText.Colour = Color4.White;
                    hitsoundChooser.placeholderText.Text = "";
                    hitsoundChooser.placeholderText.Alpha = 0;
                });
            }
            else
            {
                Schedule(() =>
                {

                    hitsoundDeleteButton.Enabled.Value = false;
                    hitsoundDeleteButton.Action = null;
                    hitsoundChooser.filenameText.Text = "";
                    hitsoundChooser.filenameText.Colour = colourProvider.Foreground1;
                    hitsoundChooser.placeholderText.Text = "Click to add a hitsound";
                    hitsoundChooser.placeholderText.Alpha = 1;
                });
            }
        }

        private partial class HitsoundDeleteButton : OsuButton
        {
            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider, OsuColour  colours)
            {
                
                Origin = Anchor.TopRight;
                Anchor = Anchor.TopRight;
                Size = new Vector2(118, 30);


                Colour = colourProvider.Colour1;
                BackgroundColour = colours.Red4;
                
                SpriteText.Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold);

                Content.CornerRadius = 8;
                
                Text = "Remove hitsound";
            }
        }
    }
}

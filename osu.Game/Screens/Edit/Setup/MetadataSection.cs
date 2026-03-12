// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Input;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Localisation;

namespace osu.Game.Screens.Edit.Setup
{
    public partial class MetadataSection : SetupSection
    {
        protected FormTextBox ArtistTextBox = null!;
        protected FormTextBox RomanisedArtistTextBox = null!;

        protected FormTextBox TitleTextBox = null!;
        protected FormTextBox RomanisedTitleTextBox = null!;

        private FormTextBox creatorTextBox = null!;
        private FormTextBox difficultyTextBox = null!;
        private FormTextBox sourceTextBox = null!;
        private FormTextBox tagsTextBox = null!;
        
        // FIX: Add level, flavor text and attributes as inputs
        private FormTextBox levelTextBox = null!;
        private FormTextBox flavorTextTextBox = null!;
        private FormTextBox attributesTextBox = null!;

        private bool reloading;
        private bool dirty;

        public override LocalisableString Title => EditorSetupStrings.MetadataHeader;

        [Resolved]
        private Editor? editor { get; set; }

        [BackgroundDependencyLoader]
        private void load(SetupScreen? setupScreen)
        {
            Children = new[]
            {
                ArtistTextBox = createTextBox<FormTextBox>(EditorSetupStrings.Artist),
                
                // Disabled
                RomanisedArtistTextBox = createTextBox<FormRomanisedTextBox>(EditorSetupStrings.RomanisedArtist),
                
                TitleTextBox = createTextBox<FormTextBox>(EditorSetupStrings.Title),
                
                // Disabled
                RomanisedTitleTextBox = createTextBox<FormRomanisedTextBox>(EditorSetupStrings.RomanisedTitle),
                
                creatorTextBox = createTextBox<FormTextBox>(EditorSetupStrings.Creator),
                difficultyTextBox = createTextBox<FormTextBox>(EditorSetupStrings.DifficultyName),
                
                // These are disabled
                sourceTextBox = createTextBox<FormTextBox>(BeatmapsetsStrings.ShowInfoSource),
                tagsTextBox = createTextBox<FormTextBox>(BeatmapsetsStrings.ShowInfoMapperTags),
                
                levelTextBox = createTextBox<FormTextBox>("Level"),
                flavorTextTextBox = createTextBox<FormTextBox>("Flavor Text"),
                attributesTextBox = createTextBox<FormTextBox>("Attributes"),
                
            };
            
            // FIX: Hide romanised input fields as they're not commonly used
            RomanisedArtistTextBox.Alpha = 0;
            RomanisedTitleTextBox.Alpha = 0;
            
            // FIX: Hide source as well
            sourceTextBox.Alpha = 0;
            
            // FIX: Hide tags as the other level, flavor text etc. inputs set tags indirectly
            tagsTextBox.Alpha = 0;

            // Only allow numbers in level input
            levelTextBox.Current.BindValueChanged(ev =>
            {
                var level = ev.NewValue;

                if (level.Length == 0) return;
                
                if (int.TryParse(level, out var levelNum))
                {
                    levelTextBox.Current.Value = levelNum.ToString();
                }
                else
                {
                    // Revert to previous value if parsing fails
                    levelTextBox.Current.Value = ev.OldValue;
                }
            });
            
            
            if (setupScreen != null)
                setupScreen.MetadataChanged += reloadMetadata;

            reloadMetadata();
        }

        private TTextBox createTextBox<TTextBox>(LocalisableString label)
            where TTextBox : FormTextBox, new()
            => new TTextBox
            {
                Caption = label,
                TabbableContentContainer = this
            };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (string.IsNullOrEmpty(ArtistTextBox.Current.Value))
                ScheduleAfterChildren(() => GetContainingFocusManager()!.ChangeFocus(ArtistTextBox));

            ArtistTextBox.Current.BindValueChanged(artist => transferIfRomanised(artist.NewValue, RomanisedArtistTextBox));
            TitleTextBox.Current.BindValueChanged(title => transferIfRomanised(title.NewValue, RomanisedTitleTextBox));

            foreach (var item in Children.OfType<FormTextBox>())
            {
                // Apply immediately on any change to ensure that if the user hits Ctrl+S after making a change (without committing)
                // it will still apply to the beatmap.
                item.Current.BindValueChanged(_ => applyMetadata());
                item.OnCommit += (_, newText) =>
                {
                    if (newText && dirty)
                        Beatmap.SaveState();
                };
            }

            if (editor != null)
                editor.Saved += () => dirty = false;

            updateReadOnlyState();
        }

        private void transferIfRomanised(string value, FormTextBox target)
        {
            if (MetadataUtils.IsRomanised(value))
                target.Current.Value = value;

            updateReadOnlyState();
        }

        private void updateReadOnlyState()
        {
            RomanisedArtistTextBox.ReadOnly = MetadataUtils.IsRomanised(ArtistTextBox.Current.Value);
            RomanisedTitleTextBox.ReadOnly = MetadataUtils.IsRomanised(TitleTextBox.Current.Value);
        }

        private void reloadMetadata()
        {
            reloading = true;

            var metadata = Beatmap.Metadata;

            RomanisedArtistTextBox.ReadOnly = false;
            RomanisedTitleTextBox.ReadOnly = false;

            ArtistTextBox.Current.Value = !string.IsNullOrEmpty(metadata.ArtistUnicode) ? metadata.ArtistUnicode : metadata.Artist;
            RomanisedArtistTextBox.Current.Value = !string.IsNullOrEmpty(metadata.Artist) ? metadata.Artist : MetadataUtils.StripNonRomanisedCharacters(metadata.ArtistUnicode);
            TitleTextBox.Current.Value = !string.IsNullOrEmpty(metadata.TitleUnicode) ? metadata.TitleUnicode : metadata.Title;
            RomanisedTitleTextBox.Current.Value = !string.IsNullOrEmpty(metadata.Title) ? metadata.Title : MetadataUtils.StripNonRomanisedCharacters(metadata.TitleUnicode);
            creatorTextBox.Current.Value = metadata.Author.Username;
            difficultyTextBox.Current.Value = Beatmap.BeatmapInfo.DifficultyName;
            sourceTextBox.Current.Value = metadata.Source;
            tagsTextBox.Current.Value = metadata.Tags;
            
            // FIX: Convert tags data back to individual fields
            var tagsData = parseTags(metadata.Tags);
            levelTextBox.Current.Value = tagsData.Level?.ToString() ?? string.Empty;
            flavorTextTextBox.Current.Value = tagsData.FlavorText ?? string.Empty;
            attributesTextBox.Current.Value = tagsData.Attributes ?? string.Empty;

            updateReadOnlyState();

            reloading = false;
        }

        private void applyMetadata()
        {
            if (reloading)
                return;

            Beatmap.Metadata.ArtistUnicode = ArtistTextBox.Current.Value;
            Beatmap.Metadata.Artist = RomanisedArtistTextBox.Current.Value;
            Beatmap.Metadata.TitleUnicode = TitleTextBox.Current.Value;
            Beatmap.Metadata.Title = RomanisedTitleTextBox.Current.Value;
            Beatmap.Metadata.Author.Username = creatorTextBox.Current.Value;
            Beatmap.BeatmapInfo.DifficultyName = difficultyTextBox.Current.Value;
            Beatmap.Metadata.Source = sourceTextBox.Current.Value;
            
            // Serialize json data
            Beatmap.Metadata.Tags = serializeTags(new TagsData
            {
                Level = int.TryParse(levelTextBox.Current.Value, out var levelVal) ? levelVal : null,
                FlavorText = string.IsNullOrEmpty(flavorTextTextBox.Current.Value) ? null : flavorTextTextBox.Current.Value,
                Attributes = string.IsNullOrEmpty(attributesTextBox.Current.Value) ? null : attributesTextBox.Current.Value,
            });
            

            dirty = true;
        }

        private class TagsData
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? Level { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? FlavorText { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? Attributes { get; set; }
        }

        private TagsData parseTags(string tags)
        {
            if (string.IsNullOrEmpty(tags))
                return new TagsData();

            try
            {
                return JsonConvert.DeserializeObject<TagsData>(tags) ?? new TagsData();
            }
            catch (JsonException e)
            {
                Logger.Error(e, "Failed to parse tags data");
                return new TagsData();
            }
        }

        private string serializeTags(TagsData data)
        {
            // Returns empty string if all fields are null/empty
            if (data.Level == null && string.IsNullOrEmpty(data.FlavorText) && string.IsNullOrEmpty(data.Attributes))
                return string.Empty;

            return JsonConvert.SerializeObject(data);
        }

        private partial class FormRomanisedTextBox : FormTextBox
        {
            internal override InnerTextBox CreateTextBox() => new RomanisedTextBox();

            private partial class RomanisedTextBox : InnerTextBox
            {
                public RomanisedTextBox()
                {
                    InputProperties = new TextInputProperties(TextInputType.Text, false);
                }

                protected override bool CanAddCharacter(char character)
                    => MetadataUtils.IsRomanised(character);
            }
        }
    }
}

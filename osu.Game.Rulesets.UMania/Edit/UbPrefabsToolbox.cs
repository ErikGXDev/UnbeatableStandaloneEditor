using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.IO;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.UMania.Beatmaps;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit;

public partial class UbPrefabsToolbox : EditorToolboxGroup
{
    private const float slider_spacing = 5;
        
    [Resolved]
    private OverlayColourProvider colourProvider { get; set; }
    
    [Resolved]
    private OsuColour colours { get; set; }
    
    [Resolved]
    private EditorBeatmap editorBeatmap { get; set; }
    
    [Resolved]
    private EditorClock editorClock { get; set; }
    
    [Resolved]
    private Storage storage { get; set; }
  

    
    private Storage snippetsStorage => storage.GetStorageForDirectory("snippets");
    
    private UbNoteBuilder noteBuilder = new UbNoteBuilder(null);
    private bool writeLock = false;
    
    private BindableBool newBlockInputActive = new BindableBool(false);
    private Bindable<string> newBlockName = new Bindable<string>(string.Empty);
    private BindableBool newBlockActionEnabled = new BindableBool(false);
    private BindableBool newBlockBeatSnapped = new BindableBool(true);

    public UbPrefabsToolbox() : base("Saved Snippets")
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
    }
    
    private Container addNoteBlockContainer;
    private SquaredRoundedButton addNewBlockButton;
    private SquaredRoundedButton addNewBeatSnappedButton;
    private FillFlowContainer snippetListContainer;

    [BackgroundDependencyLoader]
    private void load(OverlayColourProvider colourProvider)
    {
        
        Children = new Drawable[]
        {
            new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, slider_spacing),
                Padding = new MarginPadding(5),
                Children = new Drawable[]
                {
                    // form input box with "+" button next to it
                    addNoteBlockContainer = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Alpha = 0,
                        Children = new Drawable[]
                        {
                            new FormTextBox
                            {
                                Caption = "New snippet name",
                                RelativeSizeAxes = Axes.X,
                                Current = newBlockName,
                            },
                            addNewBeatSnappedButton = new SquaredRoundedButton()
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                Width = 20,
                                Height = 20,
                                Margin = new MarginPadding { Right = 5, Top = 5},
                                BackgroundColour = colours.Orange4,
                                HasTriangles = false,
                                Text = newBlockBeatSnapped.Value ? "B" : "T",
                                TooltipText = newBlockBeatSnapped.Value ? "Saved as beat offsets" : "Saved as time offsets",
                                Action = () =>
                                {
                                    newBlockBeatSnapped.Value = !newBlockBeatSnapped.Value;
                                    addNewBeatSnappedButton.Text = newBlockBeatSnapped.Value ? "B" : "T";
                                    addNewBeatSnappedButton.BackgroundColour = newBlockBeatSnapped.Value ? colours.Orange4 : colours.Blue4;
                                    addNewBeatSnappedButton.TooltipText = newBlockBeatSnapped.Value
                                        ? "Saved as beat offsets"
                                        : "Saved as time offsets";
                                }
                            },
                            addNewBlockButton = new SquaredRoundedButton()
                            {
                                Anchor = Anchor.BottomRight,
                                Origin = Anchor.BottomRight,
                                Width = 20,
                                Height = 20,
                                Margin = new MarginPadding { Right = 5, Bottom = 5},
                                BackgroundColour = colourProvider.Colour4,
                                HasTriangles = false,
                                Text = "+",
                                Enabled = { BindTarget = newBlockActionEnabled },
                                Action = () =>
                                {
                                    addNewBlock(newBlockName.Value, newBlockBeatSnapped.Value);
                                    newBlockName.Value = string.Empty;
                                    rebuildSnippetList();
                                }
                            }
                        }
                    },
                    snippetListContainer = new FillFlowContainer()
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, slider_spacing),
                    },
                    new OsuTextFlowContainer(t =>
                    {
                        t.Font = OsuFont.Default.With(size: 12, weight: FontWeight.Regular);
                    })
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Text = "Tip: Select some notes to start creating a snippet!",
                        Colour = colourProvider.Content1.Opacity(0.8f),
                        Margin = new MarginPadding { Top = 4 },
                    }
                    /*new RoundedButton()
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 30,
                        Origin = Anchor.TopCentre,
                        Anchor = Anchor.TopCentre,
                        Scale = new Vector2(0.8f),
                        BackgroundColour = colourProvider.Background4,
                        HasTriangles = false,
                        Text = "Remove blind data",
                        TooltipText = "Consider re-placing the note, if removing data through this button breaks things.",
                        Action = removeFromSelection
                    }*/
                }
            }
        };
        
        newBlockInputActive.BindValueChanged(v =>
        {
            addNoteBlockContainer.Alpha = v.NewValue ? 1 : 0;
        }, true);
        
        newBlockName.BindValueChanged(v =>
        {
            if (v.NewValue.Length > 0)
            {
                newBlockActionEnabled.Value = true;
            }
            else
            {
                newBlockActionEnabled.Value = false;
            }
            
            if (v.NewValue.Length > 30)
            {
                newBlockName.Value = v.OldValue;
            }
        }, true);
        
        editorBeatmap.SelectedHitObjects.CollectionChanged += selectedCollectionChanged;
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();
        rebuildSnippetList();
    }

    protected override void Dispose(bool isDisposing)
    {
        editorBeatmap.SelectedHitObjects.CollectionChanged -= selectedCollectionChanged;
    }
    
    private void selectedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        updateActiveState();
    }
    
    private void updateActiveState()
    {
        
        var selected = editorBeatmap.SelectedHitObjects.OfType<ManiaHitObject>().ToList();

        if (selected.Count > 0)
        {
            foreach (var b in selected)
            {
                noteBuilder.ChangeHitObject(b);

                //var icon = noteBuilder.InferObjectTypeIcon();
            }
           
            newBlockInputActive.Value = true;
            
            writeLock = true;
            
            Schedule(() => writeLock = false);
            
        }
        else
        {
            newBlockInputActive.Value = false;
            noteBuilder.ChangeHitObject(null);
        }
    }

    private void addNewBlock(string presetName, bool beatSnapped)
    {
        var selected = editorBeatmap.SelectedHitObjects.OfType<ManiaHitObject>().ToList();
        
        var maniaBeatmap = new ManiaBeatmap(new StageDefinition(6));
        
        maniaBeatmap.HitObjects.AddRange(selected);
        
        var passthrough = new PassBeatmapConverter(maniaBeatmap, new UManiaRuleset(), false, 0);
        var converted = passthrough.ConvertBeatmap(maniaBeatmap, CancellationToken.None);
        
        // To make these hitobjects reusable on different bpms, change the times to be written as 
        // beat offsets instead of milliseconds.
        var editorTime = editorClock.CurrentTime;
        var snappedEditorTime = editorBeatmap.SnapTime(editorTime, null);
        var beatLength = editorBeatmap.ControlPointInfo.TimingPointAt(snappedEditorTime).BeatLength;

        const double beatGridSize = 1.0 / 16.0;

        foreach (var h in converted.HitObjects)
        {
            var offset = h.StartTime - snappedEditorTime;

            var startTime = offset;
            
            if (beatSnapped)
            {
                var beatOffset = offset / beatLength;
                var snappedBeatOffset = Math.Round(beatOffset / beatGridSize) * beatGridSize;
                startTime = snappedBeatOffset;
            }
            h.StartTime = Math.Round(startTime, 4);

            if (h is IHasDuration d && beatSnapped)
            {
                // convert duration as well
                var durationOffset = d.Duration / beatLength;
                var snappedDurationOffset = Math.Round(durationOffset / beatGridSize) * beatGridSize;
                d.Duration = Math.Round(snappedDurationOffset, 4);
            }
        }
        
        converted.BeatmapInfo.Ruleset = new UManiaRuleset().RulesetInfo;
        
        var encoder = new LegacyBeatmapEncoder(converted, null);
        
        using var fileStream = snippetsStorage.GetStream($"{presetName.GetValidFilename()}.snippet.txt", FileAccess.Write);

        var header = "[Metadata]\nTitle: " + presetName + "\nSource: " + (beatSnapped ? "beat" : "time") + "\n\n";
        
        var headerBytes = System.Text.Encoding.UTF8.GetBytes(header);
        fileStream.Write(headerBytes, 0, headerBytes.Length);
        
        using var textWriter = new StreamWriter(fileStream);
        
        encoder.EncodeHitObjects(textWriter);

        textWriter.Flush();
        
        Logger.Log("Saved snippet to file: " + presetName.GetValidFilename() + ".snippet.txt");
        Logger.Log("Snippet stored " + converted.HitObjects.Count + " hitobjects.");
    }
    
    public record Snippet(string Name, Beatmap DataBeatmap, string fileName);
    
    private List<Snippet> loadSnippets()
    {
        var snippets = new List<Snippet>();
        
        foreach (var file in snippetsStorage.GetFiles(string.Empty, "*.snippet.txt"))
        {
            using var stream = snippetsStorage.GetStream(file);
            using var reader = new LineBufferedReader(stream);
            
            var decoder = new LegacyBeatmapDecoder();
            var beatmap = decoder.Decode(reader);
            
            /*var hitObjects = beatmap.HitObjects.OfType<ManiaHitObject>().ToList();*/

            var name = beatmap.Metadata.Title;
            
            if (string.IsNullOrEmpty(name))
            {
                name = Path.GetFileNameWithoutExtension(file);
            }
            
            snippets.Add(new Snippet(name, beatmap, file));
        }
        
        return snippets;
    }

    private partial class SnippetItem : Container
    {
        private Snippet snippet;
        private Action insertSnippet;
        private Action deleteSnippet;
        
        public SnippetItem(Snippet snippet, Action insertSnippet, Action deleteSnippet)
        {
            this.snippet = snippet;
            this.insertSnippet = insertSnippet;
            this.deleteSnippet = deleteSnippet;
            RelativeSizeAxes = Axes.X;
            Height = 30;
            Masking = true;
            CornerRadius = 5;
        }
        
        private bool deletePressOnce = false;
        
        private SquaredRoundedButton deleteButton;
        
        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, OsuColour colours)
        {
            
            var isBeatSnapped = snippet.DataBeatmap.Metadata.Source == "beat";
            
            var color = isBeatSnapped ? colours.Orange3 : colours.Blue3;
            var darkColor = isBeatSnapped ? colours.Orange4 : colours.Blue4;
            
            Children = new Drawable[]
            {
                new Box() { Colour = colourProvider.Background3, RelativeSizeAxes = Axes.Both },
                new Box() { Colour = color, Width = 12, Height = 12, Rotation = 45, Anchor = Anchor.TopRight, Origin = Anchor.Centre},
                new Box()
                {
                    Colour = ColourInfo.GradientHorizontal(colourProvider.Background3, darkColor.Opacity(0.35f)),
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Width = 80,
                    RelativeSizeAxes = Axes.Y,
                },
                new OsuSpriteText()
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 8 },
                    Text = snippet.Name,
                    Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular)
                },
                new SquaredRoundedButton()
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Width = 20,
                    Height = 20,
                    Margin = new MarginPadding { Right = 28 },
                    BackgroundColour = colourProvider.Colour4,
                    HasTriangles = false,
                    Text = ">",
                    TooltipText = "Insert Snippet",
                    Action = () =>
                    {
                        insertSnippet();
                    }
                },
                deleteButton = new SquaredRoundedButton()
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Width = 20,
                    Height = 20,
                    Margin = new MarginPadding { Right = 5 },
                    BackgroundColour = colours.Red3,
                    HasTriangles = false,
                    Text = "X",
                    TooltipText = "Delete Snippet (!)",
                    Action = () =>
                    {
                        if (!deletePressOnce)
                        {
                            deletePressOnce = true;
                            deleteButton.BackgroundColour = colours.Red4;
                            deleteButton.Text = "!!";
                            deleteButton.TooltipText = "Click again to confirm deletion";
                            
                            Scheduler.AddDelayed(() =>
                            {
                                deletePressOnce = false;
                                deleteButton.BackgroundColour = colours.Red3;
                                deleteButton.Text = "X";
                                deleteButton.TooltipText = "Delete Snippet (!)";
                            }, 2000);
                        }
                        else
                        {
                            deleteSnippet();
                        }
                    }
                }
            };
        }
    }
    
    private void rebuildSnippetList()
    {
        snippetListContainer.Clear();
        
        var snippets = loadSnippets();
        
        foreach (var snippet in snippets)
        {
            var button = new SnippetItem(snippet,
                () => { insertSnippet(snippet); },
                () =>
                {
                    deleteSnippet(snippet); 
                    rebuildSnippetList(); 
                });
            
            snippetListContainer.Add(button);
        }
    }
    
    private void insertSnippet(Snippet snippet)
    {
        Logger.Log("Inserting snippet: " + snippet.Name + " with " + snippet.DataBeatmap.HitObjects.Count + " hitobjects.");
        
        snippet.DataBeatmap.BeatmapInfo.Ruleset = new UManiaRuleset().RulesetInfo;
        
        var maniaBeatmapConverter = new ManiaBeatmapConverter(snippet.DataBeatmap, new UManiaRuleset());
        
        var maniaConvert = maniaBeatmapConverter.Convert(CancellationToken.None);
        
        var passthrough = new PassBeatmapConverter(maniaConvert, new UManiaRuleset(), false, 0);
        var converted = passthrough.ConvertBeatmap(maniaConvert, CancellationToken.None);
        
        editorBeatmap.BeginChange();
        
        var snappedEditorTime = editorBeatmap.SnapTime(editorClock.CurrentTime, null);
        var beatLength = editorBeatmap.ControlPointInfo.TimingPointAt(snappedEditorTime).BeatLength;
        var savedAsBeats = snippet.DataBeatmap.Metadata.Source == "beat";
        
        foreach (var h in converted.HitObjects)
        {
            if (savedAsBeats)
                h.StartTime *= beatLength; // source = saved as beats
            h.StartTime += snappedEditorTime;
            
            if (h is IHasDuration d && savedAsBeats)
            {
                d.Duration *= beatLength;
            }
            editorBeatmap.Add(h);
        }
        
        editorBeatmap.EndChange();
        
    }
    
    private void deleteSnippet(Snippet snippet)
    {
        snippetsStorage.Delete(snippet.fileName);
    }

    public partial class SquaredRoundedButton : RoundedButton
    {
        public SquaredRoundedButton()
        {
        }

        public float CCornerRadius = 4;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            
            Content.CornerRadius = CCornerRadius;
            SpriteText.Y = -1;
        }
    }

}
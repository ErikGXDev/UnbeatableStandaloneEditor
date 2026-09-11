// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.IO;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Setup;
using osuTK;
using FileInfo = System.IO.FileInfo;

namespace osu.Game.Screens.Edit.Timing
{
    public partial class ControlPointList : CompositeDrawable
    {
        public Action? SelectClosestTimingPoint { get; init; }

        private ControlPointTable table = null!;
        private Container controls = null!;
        private OsuButton deleteButton = null!;
        private RoundedButton addButton = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        protected EditorBeatmap Beatmap { get; private set; } = null!;
        
        [Resolved]
        protected Editor editor { get; private set; } = null!;

        [Resolved]
        private Bindable<ControlPointGroup?> selectedGroup { get; set; } = null!;

        [Resolved]
        private IEditorChangeHandler? editorChangeHandler { get; set; }
        
        private ResourcesSection resourcesSection = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.Both;

            const float margins = 10;
            InternalChildren = new Drawable[]
            {
                createDebugMenu(),
                createExplanation(),
                
                table = new ControlPointTable
                {
                    RelativeSizeAxes = Axes.Both,
                    Groups = { BindTarget = Beatmap.ControlPointInfo.Groups, },
                },
                controls = new Container
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background2,
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Padding = new MarginPadding { Left = margins, Vertical = margins * 2, },
                            Children = new Drawable[]
                            {
                                new RoundedButton
                                {
                                    Text = "Select closest to current time",
                                    Action = SelectClosestTimingPoint,
                                    Size = new Vector2(220, 30),
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Enabled = { Value = false }
                                },
                            }
                        },
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Masking = true,
                            CornerRadius = 6,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = colourProvider.Background4,
                                },
                                new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Padding = new MarginPadding(4),
                                    Child = new TimingAdjustButton(1)
                                    {
                                        Text = "Offset all points",
                                        Scale = new Vector2(1.75f),
                                        Action = offset =>
                                        {
                                            var selected = selectedGroup.Value;
                                            double selectedTime = selected?.Time ?? double.NaN;

                                            Beatmap.BeginChange();
                                            TimingSectionAdjustments.ApplyGlobalOffset(Beatmap, offset);
                                            Beatmap.EndChange();

                                            // The selected group was removed and re-added at a new time, so re-point at it.
                                            if (selected != null)
                                                selectedGroup.Value = Beatmap.ControlPointInfo.GroupAt(selectedTime + offset);
                                        },
                                        Size = new Vector2(125, 30),
                                    },
                                },
                            },
                        },
                        resourcesSection = new ResourcesSection
                        {
                            Alpha = 0f,
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Spacing = new Vector2(5),
                            Padding = new MarginPadding { Right = margins, Vertical = margins, },
                            Children = new Drawable[]
                            {
                                deleteButton = new RoundedButton
                                {
                                    Text = "-",
                                    Size = new Vector2(30, 30),
                                    Action = delete,
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    BackgroundColour = colours.Red3,
                                    Enabled = { Value = false }
                                },
                                addButton = new RoundedButton
                                {
                                    Action = addNew,
                                    Size = new Vector2(160, 30),
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Enabled = { Value = false }
                                },
                            }
                        },
                    }
                },
            };

            if (editorChangeHandler != null)
                editorChangeHandler.OnStateChange += onUndoRedo;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            selectedGroup.BindValueChanged(selected =>
            {
                deleteButton.Enabled.Value = selected.NewValue != null;

                addButton.Text = selected.NewValue != null
                    ? "+ Clone to current time"
                    : "+ Add at current time";
            }, true);
            
            LoadableBeatmaps = GetAllLoadableBeatmaps();
        }

        protected override bool OnClick(ClickEvent e)
        {
            selectedGroup.Value = null;
            return true;
        }

        protected override void Update()
        {
            base.Update();

            addButton.Enabled.Value = false/*clock.CurrentTimeAccurate != selectedGroup.Value?.Time*/;
            table.Padding = new MarginPadding { Bottom = controls.DrawHeight };
            
            // Continually update debug text with current beatmap title and timing point deltas
            if (currentBeatmap != null && TimingPointOriginalTimes.Count > 0)
            {
                string debugTextContent = currentBeatmap.Metadata.Title + " (" + (CurrentMapIndex + 1) + "/" + LoadableBeatmaps.Count + ")";

                /*for (int i = 0; i < TimingPointOriginalTimes.Count; i++)
                {
                    var originalTime = TimingPointOriginalTimes[i];
                    var currentTime = currentBeatmap.ControlPointInfo.TimingPoints[i].Time;
                    var delta = currentTime - originalTime;

                    debugTextContent += " | " + delta.ToString("F0");
                }*/

                if (TimingPointOriginalTimes.Count > 0)
                {
                    var originalTime = TimingPointOriginalTimes[0];
                    var currentTime = currentBeatmap.ControlPointInfo.TimingPoints[0].Time;
                    var delta = currentTime - originalTime;

                    debugTextContent += " | Offset: " + delta.ToString("F0");
                }

                debugText.Text = debugTextContent;
            }
        }

        private void delete()
        {
            if (selectedGroup.Value == null)
                return;

            Beatmap.ControlPointInfo.RemoveGroup(selectedGroup.Value);

            selectedGroup.Value = Beatmap.ControlPointInfo.Groups.FirstOrDefault(g => g.Time >= clock.CurrentTime);
        }

        private void addNew()
        {
            bool isFirstControlPoint = !Beatmap.ControlPointInfo.TimingPoints.Any();

            var group = Beatmap.ControlPointInfo.GroupAt(clock.CurrentTime, true);

            if (isFirstControlPoint)
                group.Add(new TimingControlPoint());
            else
            {
                // Try and create matching types from the currently selected control point.
                var selected = selectedGroup.Value;

                if (selected != null && !ReferenceEquals(selected, group))
                {
                    foreach (var controlPoint in selected.ControlPoints)
                    {
                        group.Add(controlPoint.DeepClone());
                    }
                }
            }

            selectedGroup.Value = group;
        }

        private void onUndoRedo()
        {
            // Best effort. We have no tracking of control points through undo/redo changes.
            // If we don't deselect, things like offset changes could spawn groups to be added from previous states (see https://github.com/ppy/osu/issues/31098).
            if (selectedGroup.Value != null && !Beatmap.ControlPointInfo.Groups.Contains(selectedGroup.Value))
                selectedGroup.Value = null;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (editorChangeHandler != null)
                editorChangeHandler.OnStateChange -= onUndoRedo;
        }
        
        
        #region Offset fixing code

        private string source_directory => File.ReadAllText(AppContext.BaseDirectory + "/source_directory.txt").Trim();

        private string report_file => AppContext.BaseDirectory + "/report.csv";
            

        public record LoadableBeatmap (string txtFile, string oggFile, int hash = 0);

        public record PrettyBeatmap(string title, string difficultyName, string txtFile);

        public int CurrentMapIndex = -1;

        public int CurrentVersionIndex = 0;
        
        public List<double> TimingPointOriginalTimes = new List<double>();
        
        public Dictionary<int, List<PrettyBeatmap>> SatisfiedBeatmaps = new Dictionary<int, List<PrettyBeatmap>>();
        
        public List<LoadableBeatmap> LoadableBeatmaps = new List<LoadableBeatmap>();

        public Beatmap currentBeatmap;
        public LoadableBeatmap currentLoadableBeatmap;
        
        private List<LoadableBeatmap> GetAllLoadableBeatmaps()
        {
            // Find the first .txt files in all sub-folders of the source_directory, IF the folder also contains an ogg file
            // (sub-directories can also contain sub-directories)
            var beatmaps = new List<LoadableBeatmap>();
            foreach (var dir in Directory.GetDirectories(source_directory, "*", SearchOption.AllDirectories))
            {
                var txtFiles = Directory.GetFiles(dir, "*.txt", SearchOption.TopDirectoryOnly);
                var oggFiles = Directory.GetFiles(dir, "*.ogg", SearchOption.TopDirectoryOnly);

                if (txtFiles.Length > 0 && oggFiles.Length > 0)
                {
                    foreach (var txtFile in txtFiles)
                    {
                        LegacyBeatmapDecoder decoder = new LegacyBeatmapDecoder();
                
                        var lineBufferedStream = new LineBufferedReader(new FileStream(txtFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                        Beatmap b = decoder.Decode(lineBufferedStream);
                        
                        // ogg file makes sure we only have same hashes per audio as well
                        int hash = GetBeatmapTimingPointHash(b, oggFiles[0]);

                        if (beatmaps.All(bm => bm.hash != hash))
                        {
                            beatmaps.Add(new LoadableBeatmap(txtFile, oggFiles[0], hash));
                        }
                        
                        SatisfiedBeatmaps.TryAdd(hash, new List<PrettyBeatmap>());
                        SatisfiedBeatmaps[hash].Add(new PrettyBeatmap(b.Metadata.Title, b.BeatmapInfo.DifficultyName, txtFile));
                    }
                }
            }
            
            
            return beatmaps;
        }
        
        private int GetBeatmapTimingPointHash(Beatmap beatmap, string additionalData = "")
        {
            int hash = 17;

            foreach (var timingPoint in beatmap.ControlPointInfo.TimingPoints)
            {
                hash = hash * 31 + timingPoint.Time.GetHashCode();
                hash = hash * 31 + timingPoint.BeatLength.GetHashCode();
            }
            
            if (!string.IsNullOrEmpty(additionalData))
                hash = hash * 31 + additionalData.GetHashCode();
            
            return hash;
        }
        
        public void SaveAndLoadNextBeatmap()
        {
            // Save current changes
            if (CurrentMapIndex > -1 && TimingPointOriginalTimes.Count > 0)
            {
                var hash = currentLoadableBeatmap.hash;
                
                foreach (var satisfiedBeatmap in SatisfiedBeatmaps[hash])
                {
                    var result = satisfiedBeatmap.title + "/" + satisfiedBeatmap.difficultyName;
                    
                    /*for (int i = 0; i < TimingPointOriginalTimes.Count; i++)
                    {
                        var originalTime = TimingPointOriginalTimes[i];
                        var currentTime = currentBeatmap.ControlPointInfo.TimingPoints[i].Time;
                        var delta = currentTime - originalTime;

                        result += ";" + delta.ToString("F0");
                    }*/
                    
                    if (TimingPointOriginalTimes.Count > 0)
                    {
                        var originalTime = TimingPointOriginalTimes[0];
                        var currentTime = currentBeatmap.ControlPointInfo.TimingPoints[0].Time;
                        var delta = currentTime - originalTime;

                        result += ";" + delta.ToString("F0");
                    }
                    
                    File.AppendAllText(report_file, result + "\n");
                }
                
            }

            saveAndLoadNextButton.Text = "Save timing and load next";
            skipCurrentButton.Enabled.Value = true;
            reloadCurrentButton.Enabled.Value = true;
            swapVersionButton.Enabled.Value = true;
            
            SwitchNextBeatmap();
        }

        // Above, but without saving the current changes
        public void SwitchNextBeatmap()
        {
            // Load new
            CurrentMapIndex++;
            if (CurrentMapIndex >= LoadableBeatmaps.Count)
            {
                CurrentMapIndex = 0;
            }
            
            LoadCurrentBeatmap();
        }

        public void LoadCurrentBeatmap()
        {

            if (CurrentMapIndex == -1)
            {
                return;
            }
            
            TimingPointOriginalTimes.Clear();
            CurrentVersionIndex = 0;

            currentLoadableBeatmap = LoadableBeatmaps[CurrentMapIndex];
            
            updateVersionText();
            
            Console.WriteLine($"Loading beatmap: {currentLoadableBeatmap.oggFile} and {currentLoadableBeatmap.oggFile}");
            
            
            // Load the beatmap
            
            FileInfo txtFile = new FileInfo(currentLoadableBeatmap.txtFile);
            FileInfo oggFile = new FileInfo(currentLoadableBeatmap.oggFile);

            resourcesSection.ChangeAudioTrack(oggFile, true);
            
            // Loading twice but dont care
            LegacyBeatmapDecoder decoder = new LegacyBeatmapDecoder();
            
            var lineBufferedStream = new LineBufferedReader(new FileStream(txtFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            Beatmap b = decoder.Decode(lineBufferedStream);
            
            currentBeatmap = b;
            
            Beatmap.ControlPointInfo.Clear();
            
            foreach (var group in b.ControlPointInfo.Groups)
            {
                TimingPointOriginalTimes.Add(group.Time);
                
                foreach (var controlPoint in group.ControlPoints)
                {
                    Beatmap.ControlPointInfo.Add(controlPoint.Time, controlPoint);
                }
            }

            applyHitObjectsFromBeatmap(b);

            debugText.Text = b.Metadata.Title + " (" + CurrentMapIndex + 1 + "/" + LoadableBeatmaps.Count + ")";
        }

        // Switch hitobjects between satisfied beatmaps to check out multiple versions
        public void SwitchNextBeatmapInsideLoadable()
        {
            var hash = currentLoadableBeatmap.hash;
            
            var satisfiedBeatmaps = SatisfiedBeatmaps[hash];
            
            CurrentVersionIndex++;
            if (CurrentVersionIndex >= satisfiedBeatmaps.Count)
            {
                CurrentVersionIndex = 0;
            }
            
            var current = satisfiedBeatmaps[CurrentVersionIndex];
            
            var decoder = new LegacyBeatmapDecoder();
            
            var lineBufferedStream = new LineBufferedReader(new FileStream(current.txtFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            
            Beatmap b = decoder.Decode(lineBufferedStream);
            
            applyHitObjectsFromBeatmap(b);

            updateVersionText();
        }
        
        public void StartFromZero()
        {
            File.WriteAllText(report_file, "");
            CurrentMapIndex = -1;
            CurrentVersionIndex = 0;
            TimingPointOriginalTimes.Clear();
            debugText.Text = "No beatmap selected yet";
            versionText.Text = "...";
            saveAndLoadNextButton.Text = "Load first beatmap";
            reloadCurrentButton.Enabled.Value = false;
            skipCurrentButton.Enabled.Value = false;
            swapVersionButton.Enabled.Value = false;
        }

        private void applyHitObjectsFromBeatmap(IBeatmap beatmap)
        {
            beatmap.BeatmapInfo.Ruleset = Beatmap.BeatmapInfo.Ruleset;
            
            var maniaConverter = Beatmap.BeatmapInfo.Ruleset.CreateInstance().CreateBeatmapConverter(beatmap);
            
            var maniaBeatmap = maniaConverter.Convert();
            
            ((IList)Beatmap.HitObjects).Clear();
            
            Beatmap.UpdateAllHitObjects();

            Schedule(() =>
            {
                foreach (var hitObject in maniaBeatmap.HitObjects)
                {
                    ((IList)Beatmap.HitObjects).Add(hitObject);
                }
            
                Beatmap.UpdateAllHitObjects();
                
                editor.ReloadComposeScreen();
            });
            
            
        }

        private void updateVersionText()
        {
            var hash = currentLoadableBeatmap.hash;
            
            var satisfiedBeatmaps = SatisfiedBeatmaps[hash];
            var current = satisfiedBeatmaps[CurrentVersionIndex];
            
            versionText.Text = "Version: " + (CurrentVersionIndex + 1) + "/" + satisfiedBeatmaps.Count + " (" + current.difficultyName + ")";
        }
        
        private OsuSpriteText debugText = null!;
        private OsuSpriteText versionText = null!;

        private Drawable createDebugMenu()
        {
            var container = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                X = -80,
                Width = 350,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding { Left = 10, Top = 10, },
                Depth = -10,
                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Colour4.Black,
                        Alpha = 0.5f,
                    },
                    new FillFlowContainer()
                    {
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(6),
                        Padding = new MarginPadding(4),
                        Children = new Drawable[] {
                           
                            debugText = new OsuSpriteText()
                            {
                                Text = "No beatmap selected yet",
                                Font = OsuFont.Default.With(size: 16, weight: FontWeight.Bold),
                            },
                            versionText = new OsuSpriteText()
                            {
                                Text = "...",
                                Font = OsuFont.Default.With(size: 14, weight: FontWeight.Bold),
                            },
                            saveAndLoadNextButton = new RoundedButton
                            {
                                Text = "Load first beatmap",
                                TooltipText = "Loads the next beatmap so you can edit its timing points. Saves the timing point changes in a report file.",
                                Size = new Vector2(200, 30),
                                Action = SaveAndLoadNextBeatmap,
                                Enabled = { Value = true }
                            },
                            reloadCurrentButton = new RoundedButton()
                            {
                                Text = "Reload current beatmap",
                                TooltipText = "For when you want to reset your timing changes.",
                                Size = new Vector2(200, 30),
                                BackgroundColour = Colour4.Orange,
                                Action = LoadCurrentBeatmap,
                                Enabled = { Value = false }
                            },
                            swapVersionButton = new RoundedButton()
                            {
                                Text = "Swap beatmap version",
                                TooltipText = "Change the beatmap difficulty, if there are multiple versions that all have the same timing points. Changes the hitobjects in the compose tab.\nWARNING: The hitobjects will probably be unsnapped if you already moved the timing points.",
                                Size = new Vector2(200, 30),
                                BackgroundColour = Colour4.Orange,
                                Action = SwitchNextBeatmapInsideLoadable,
                                Enabled = { Value = false }
                            },
                            skipCurrentButton = new RoundedButton()
                            {
                                Text = "Skip this beatmap",
                                TooltipText = "Skip this beatmap without saving changes to the report file.",
                                Size = new Vector2(200, 30),
                                BackgroundColour = Colour4.Orange,
                                Action = SwitchNextBeatmap,
                                Enabled = { Value = false }
                            },
                            startNewButton = new RoundedButton()
                            {
                                Text = "Start from zero",
                                TooltipText = "Clears the report file and starts the timing point editing from scratch. Use this if you want to start a new report file.",
                                Size = new Vector2(200, 30),
                                BackgroundColour = Colour4.Red,
                                Action = StartFromZero,
                                Enabled = { Value = true }
                            },
                        }
                    }
                 }
            };

            return container;
        }

        private Drawable createExplanation()
        {
            var container = new Container()
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                Width = 300,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding { Right = 10 },
                Depth = -10,
                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Colour4.Black,
                        Alpha = 0.5f,
                    },
                   
                    new OsuTextFlowContainer(t =>
                    {
                        t.Font = OsuFont.Default.With(size: 16);
                        t.AllowMultiline = true;
                    })
                    {
                        Text = "Usage instructions\n\n1. Press the green button to load your first beatmap.\n2. Adjust the timing points, so they fit as close as possible to the waveform you see at the top. Use the \"Offset all points\" input at the bottom to adjust. You can also check out the compose tab, to see the notes as well.\n(Optional) Press \"Swap beatmap version\" button to switch between chart difficulties.\n3. When you are done adjusting the timing points, press the green button again to save your timing point changes and load the next one.\n4. Repeat until you are done with all beatmaps.\n5. Find your report of all your offset changes in a \"report.csv\" file in the directory of where you opened the editor from.\n6. If you want to try again, first move or rename the report file, to prevent adding duplicate entries to it.",
                        Width = 300,
                        Padding = new MarginPadding(4),
                        AutoSizeAxes = Axes.Y,
                    }
                }
                    
            };
            
            return container;
        }
        
        private RoundedButton saveAndLoadNextButton = null!;
        private RoundedButton reloadCurrentButton = null!;
        private RoundedButton skipCurrentButton = null!;
        private RoundedButton swapVersionButton = null!;
        private RoundedButton startNewButton = null!;
        
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Audio;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Edit.Composition;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Rulesets.UMania.Objects.Drawables;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.RadioButtons;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osu.Framework.Testing;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit;

[Cached]
public partial class UnbeatableHitObjectComposer : ManiaHitObjectComposer
{
    public UnbeatableHitObjectComposer(Ruleset ruleset)
        : base(ruleset)
    {
    }
    
    [Resolved] private OsuConfigManager config { get; set; } = null!;

    public bool Is4Key => config.Get<bool>(OsuSetting.Editor4KeyMode);

    public Bindable<bool> KeyBasedCharting { get; private set; } = null!;

    private KeyBasedChartingHandler keyBasedChartingHandler = null!;

    protected override void LoadComplete()
    {
        base.LoadComplete();

        // Add the order-toggle layer correctly so it can receive input
        PlayfieldContentContainer.Add(new UbNoteOrderButtonLayer(Playfield.Stages[0])
        {
            RelativeSizeAxes = Axes.Both,
        });

        previewArea = new UManiaPreviewArea
        {
            Anchor = Anchor.BottomRight,
            Origin = Anchor.BottomRight,
            Margin = new MarginPadding { Right = 10, Bottom = 10 },
            RightToolbox = RightToolbox,
        };
        
        PlayfieldContentContainer.Add(previewArea);

        // Timing reminder popup
        var timingPopup = new Container
        {
            Anchor = Anchor.TopCentre,
            Origin = Anchor.TopCentre,
            Y = 20,
            AutoSizeAxes = Axes.Both,
            Alpha = EditorBeatmap.HasTiming.Value ? 0 : 1,
            Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    CornerRadius = 8,
                    Masking = true,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Colour4.Black.Opacity(0.75f),
                    },
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding(16),
                    Spacing = new Vector2(0, 4),
                    Children = new Drawable[]
                    {
                        new SpriteIcon
                        {
                            Icon = FontAwesome.Solid.Clock,
                            Size = new Vector2(24),
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Colour = Colour4.Orange,
                        },
                        new SpriteText
                        {
                            Text = "No timing points set",
                            Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Colour = Colour4.White,
                        },
                        new SpriteText
                        {
                            Text = "Go to the Timing tab to add one",
                            Font = OsuFont.GetFont(size: 13),
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Colour = Colour4.White.Opacity(0.8f),
                        },
                    },
                },
            },
        };

        PlayfieldContentContainer.Add(timingPopup);

        keyBasedChartingHandler = new KeyBasedChartingHandler
        {
            RelativeSizeAxes = Axes.Both,
        };
        PlayfieldContentContainer.Add(keyBasedChartingHandler);

        KeyBasedCharting = config.GetBindable<bool>(OsuSetting.EditorKeyBasedCharting);
        KeyBasedCharting.BindValueChanged(v =>
        {
            keyBasedChartingHandler.Enabled = v.NewValue;
            EditorKeyBasedCharting.IsActive = v.NewValue;
            SettingUseKeyCharting.Value = v.NewValue ? TernaryState.True : TernaryState.False;

            foreach (var button in toolboxCollection.ChildrenOfType<EditorRadioButton>())
            {
                if (button.Button is HitObjectCompositionToolButton toolButton)
                {
                    bool isSelect = toolButton.Tool is SelectTool;
                    button.Alpha = isSelect || !v.NewValue ? 1 : 0;
                }
            }

            // Auto-switch to select tool when enabling key-based charting on a non-select tool.
            if (v.NewValue && BlueprintContainer.CurrentTool is not SelectTool)
                SetSelectTool();
        }, true);

        SettingUseKeyCharting.BindValueChanged(v => KeyBasedCharting.Value = v.NewValue == TernaryState.True);

        hasTimingHandler = hasTiming =>
        {
            timingPopup.FadeTo(hasTiming.NewValue ? 0 : 1, 300, Easing.OutQuint);
        };
        EditorBeatmap.HasTiming.ValueChanged += hasTimingHandler;
    }
    
    private UManiaPreviewArea previewArea;

    protected override Drawable CreateHitObjectInspector() => new UManiaHitObjectInspector();

    protected override IReadOnlyList<CompositionTool> CompositionTools => new CompositionTool[]
    {
        // Unbeatable Note Predicates

        // Normal Notes
        new UbNoteCompositionTool("Note", UbIconType.Note, [2, 3], []),
        new UbHoldNoteCompositionTool("Hold", UbIconType.Hold, [2, 3], []),

        // Dodge
        new UbNoteCompositionTool("Dodge", UbIconType.Dodge, [2, 3], [HitSampleInfo.HIT_WHISTLE]),
        // Blue/Double
        new UbHoldNoteCompositionTool("Double", UbIconType.Double, [2, 3], [HitSampleInfo.HIT_WHISTLE]),
        // Freestyle
        new UbNoteCompositionTool("Freestyle", UbIconType.Freestyle, [5], []),
        // Spam
        new UbHoldNoteCompositionTool("Spam", UbIconType.Spam, [5], [HitSampleInfo.HIT_FINISH]),

        // Flip
        new UbNoteCompositionTool("Flip", UbIconType.Flip, [4], []),
        // Zoom
        new UbNoteCompositionTool("Zoom", UbIconType.Zoom, [4], [HitSampleInfo.HIT_WHISTLE]),

        // Cop
        // new UbNoteCompositionTool("Brawl", UbIconType.Brawl, [2, 3], [], HitSampleInfo.BANK_STRONG),
        // Cop Hold
        // new UbHoldNoteCompositionTool("Brawl Hold", UbIconType.Brawl, [2, 3], [], HitSampleInfo.BANK_STRONG)
    };

    public Bindable<TernaryState> SettingShowAllowedColumns = new Bindable<TernaryState>(TernaryState.True);

    public Bindable<TernaryState> SettingShowPlacementOrder = new Bindable<TernaryState>(TernaryState.True);
    
    public Bindable<TernaryState> SettingShowPreview = new Bindable<TernaryState>(TernaryState.True);

    private Bindable<TernaryState> SettingUseKeyCharting = new Bindable<TernaryState>(TernaryState.False);
    
    public DrawableTernaryButton ModFlyingButton = null!;
    public DrawableTernaryButton ModInvisibleButton = null!;

    public DrawableTernaryButton ModCopButton = null!;
    public DrawableTernaryButton ModCopFinishButton = null!;
    public DrawableTernaryButton ModCop1Button = null!;
    public DrawableTernaryButton ModCop2Button = null!;
    public DrawableTernaryButton ModCop3Button = null!;
    public DrawableTernaryButton ModCop4Button = null!;
    public DrawableTernaryButton ModCopHeavyButton = null!;


    public DrawableTernaryButton ModSwapImmediateButton = null!;



    // Create a dictionary that maps each button to a list of tool names that should have it enabled
    public List<ModMapping> ModButtonToolMap;

    // Suppresses re-applying modifiers
    private bool suppressSelectionFeedback;

    // True when the current selection contains at least one cop note
    private bool selectionContainsCop;

    private NotifyCollectionChangedEventHandler? selectionChangedHandler;
    private Action<ValueChangedEvent<bool>>? hasTimingHandler;

    // Returns the modifier buttons that are valid for the current selection
    public IEnumerable<DrawableTernaryButton> GetApplicableModifierButtons()
    {
        if (!EditorBeatmap.SelectedHitObjects.OfType<ManiaHitObject>().Any())
            yield break;

        var selection = classifySelection();

        var copSubButtons = new HashSet<DrawableTernaryButton>
        {
            ModCop1Button, ModCop2Button, ModCop3Button, ModCop4Button, ModCopFinishButton, ModCopHeavyButton,
        };

        foreach (var mapping in ModButtonToolMap)
        {
            bool appliesToSelection = selection.ToolNames.All(mapping.ApplicableTools.Contains);

            bool visible = mapping.Button switch
            {
                // Cop master: valid whenever all notes can be cop notes.
                _ when mapping.Button == ModCopButton => selection.AllCopCapable && appliesToSelection,
                // Cop sub-modifiers: only for (pure) cop selections, never for mixed or non-cop selections.
                _ when copSubButtons.Contains(mapping.Button) => selection.AllCopCapable && selection.HasCop && !selection.MixedCop && appliesToSelection,
                // Non-cop modifiers: invalid whenever a cop note is present in the selection.
                _ => !selection.HasCop && appliesToSelection,
            };

            if (visible)
                yield return mapping.Button;
        }
    }

    public void SyncModifierButtonsFromSelection() => updateButtonStatesFromSelection();

    protected override IEnumerable<Drawable> CreateTernaryButtons()
    {
        return new Drawable[]
        {
            ModFlyingButton = makeButton("Flying", FontAwesome.Solid.Wind),
            ModInvisibleButton = makeButton("Invisible", FontAwesome.Solid.EyeSlash),
            ModSwapImmediateButton = makeButton("Swap Immediate", FontAwesome.Solid.ExchangeAlt),
            ModCopButton = makeButton("Brawl Note", FontAwesome.Solid.UserShield),
            ModCop1Button = makeButton("Cop 1", FontAwesome.Solid.UserShield),
            ModCop2Button = makeButton("Cop 2", FontAwesome.Solid.UserShield),
            ModCop3Button = makeButton("Cop 3", FontAwesome.Solid.UserShield),
            ModCop4Button = makeButton("Cop 4", FontAwesome.Solid.UserShield),
            ModCopFinishButton = makeButton("Knock-out", FontAwesome.Solid.UserShield),
            ModCopHeavyButton = makeButton("Heavy Brawl", FontAwesome.Solid.UserShield),
        };
    }

    private DrawableTernaryButton makeButton(string description, IconUsage icon)
    {
        return new DrawableTernaryButton
        {
            // Fix: Make EditorRadioButton (and DrawableTernaryButton) more compact so it all fits on one page.
            Height = 32,
            
            Current = new Bindable<TernaryState>(),
            Description = description,
            CreateIcon = () => new SpriteIcon { Icon = icon },
        };
    }

    private ModMapping makeMapping(DrawableTernaryButton button, List<string> tools, Func<bool>? availabilityPredicate = null)
    {
        return new ModMapping
        {
            Button = button,
            ApplicableTools = tools,
            AvailabilityPredicate = availabilityPredicate
        };
    }

    private bool isCopModding()
    {
        return ModCopButton.Current.Value == TernaryState.True && ModCopButton.Enabled.Value;
    }
    
    private static string? iconToToolName(UbIconType icon) => icon switch
    {
        UbIconType.Note => "Note",
        UbIconType.Hold => "Hold",
        UbIconType.Dodge => "Dodge",
        UbIconType.Double => "Double",
        UbIconType.Freestyle => "Freestyle",
        UbIconType.Spam => "Spam",
        UbIconType.Flip => "Flip",
        UbIconType.Zoom => "Zoom",
        _ => null,
    };

    // Classify selection for available modifiers
    private class SelectionModifierClassification
    {
        public HashSet<string> ToolNames { get; } = new HashSet<string>();
        public bool HasCop { get; set; }
        public bool HasCopCapableNonCop { get; set; }
        public bool HasOther { get; set; }

        public bool AllCopCapable => !HasOther;
        public bool MixedCop => AllCopCapable && HasCop && HasCopCapableNonCop;
    }

    private static bool isCopIcon(UbIconType icon) =>
        icon is UbIconType.ModCop1 or UbIconType.ModCop2 or UbIconType.ModCop3 or UbIconType.ModCop4 or UbIconType.ModCopFinish or UbIconType.ModCopHeavy;

    private SelectionModifierClassification classifySelection()
    {
        var result = new SelectionModifierClassification();

        foreach (var h in EditorBeatmap.SelectedHitObjects.OfType<ManiaHitObject>())
        {
            var helper = new UbNoteBuilderHelper(this, h);

            if (helper.InferObjectModifierIcons().Any(isCopIcon))
            {
                // Cop notes only ever originate from the Note/Hold tools, so treat them as such
                // for applicability. Without this, a pure cop selection yields an empty tool set
                result.ToolNames.Add("Note");
                result.ToolNames.Add("Hold");
                result.HasCop = true;
            }
            else
            {
                var toolName = iconToToolName(helper.InferObjectTypeIcon());
                if (toolName != null)
                    result.ToolNames.Add(toolName);

                // Only Note/Hold types can be converted to cop notes
                if (toolName == "Note" || toolName == "Hold")
                    result.HasCopCapableNonCop = true;
                else
                    result.HasOther = true;
            }
        }

        return result;
    }


    [BackgroundDependencyLoader]
    private void load()
    {
        ModButtonToolMap = new List<ModMapping>()
        {
            makeMapping(ModFlyingButton, ["Note", "Hold", "Dodge", "Double"], () => !(isCopModding() || selectionContainsCop) ),
            makeMapping(ModInvisibleButton, ["Note", "Hold"], () => !(isCopModding() || selectionContainsCop) ),
            makeMapping(ModSwapImmediateButton, ["Flip"], () => !(isCopModding() || selectionContainsCop) ),
            makeMapping(ModCopButton, ["Note", "Hold"] ),
            makeMapping(ModCop1Button, ["Note", "Hold"], () => isCopModding() || selectionContainsCop),
            makeMapping(ModCop2Button, ["Note", "Hold"], () => isCopModding() || selectionContainsCop),
            makeMapping(ModCop3Button, ["Note", "Hold"], () => isCopModding() || selectionContainsCop),
            makeMapping(ModCop4Button, ["Note", "Hold"], () => isCopModding() || selectionContainsCop),
            makeMapping(ModCopFinishButton, ["Note", "Hold"], () => isCopModding() || selectionContainsCop),
            makeMapping(ModCopHeavyButton, ["Note", "Hold"], () => isCopModding() || selectionContainsCop),
        };

        ModCop1Button.Current.Value = TernaryState.True; // Default to cop 1

        // Make cop buttons mutually exclusive
        var copButtons = new[] { ModCop1Button, ModCop2Button, ModCop3Button, ModCop4Button };

        foreach (var button in copButtons)
        {
            button.Current.BindValueChanged(val =>
            {
                if (val.NewValue == TernaryState.False)
                {
                    // If none are selected, reselect this one
                    bool anySelected = false;
                    foreach (var otherButton in copButtons)
                    {
                        if (otherButton.Current.Value == TernaryState.True)
                        {
                            anySelected = true;
                            break;
                        }
                    }
                    if (!anySelected)
                        button.Current.Value = TernaryState.True;
                }

                if (val.NewValue == TernaryState.True)
                {
                    foreach (var otherButton in copButtons)
                    {
                        if (otherButton != button)
                            otherButton.Current.Value = TernaryState.False;
                    }
                }
            });
        }



        LeftToolbox.Add(new EditorToolboxGroup("unbeatable")
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Children =
                [
                    new DrawableTernaryButton
                    {
                        Current = SettingShowAllowedColumns,
                        Description = "Use column hints",
                        CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.Lightbulb },
                    },
                    new DrawableTernaryButton
                    {
                        Current = SettingShowPlacementOrder,
                        Description = "Placement order",
                        CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.Circle },
                    },
                    new DrawableTernaryButton
                    {
                        Current = SettingShowPreview,
                        Description = "Show preview",
                        CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.Tv },
                    },
                    new DrawableTernaryButton
                    {
                        Current = SettingUseKeyCharting,
                        Description = "Key-based charting",
                        TooltipText = "Press 1-6 to place notes in the corresponding column, similar to the official editor.\nHold a key and scroll to create hold notes.\nUse Shift to place a Dodge, Double or Zoom note (depending on the column).\nModifier buttons (Q-P) still apply to placed notes.\n(Experimental!)",
                        CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.Keyboard },
                    }
                ]
            },
        });

        // Wire modifier toggles to apply to selected notes
        var modButtons = new[] { ModFlyingButton, ModInvisibleButton, ModSwapImmediateButton, ModCopButton, ModCop1Button, ModCop2Button, ModCop3Button, ModCop4Button, ModCopFinishButton, ModCopHeavyButton };

        foreach (var button in modButtons)
            button.Current.BindValueChanged(_ => applyModifiersToSelection());

        selectionChangedHandler = (_, _) => Scheduler.AddOnce(updateButtonStatesFromSelection);
        EditorBeatmap.SelectedHitObjects.CollectionChanged += selectionChangedHandler;
    }

    private void applyModifiersToSelection()
    {
        if (suppressSelectionFeedback) return;

        var selected = EditorBeatmap.SelectedHitObjects.OfType<ManiaHitObject>().ToList();
        if (selected.Count == 0) return;

        EditorBeatmap.PerformOnSelection(h =>
        {
            if (h is not ManiaHitObject maniaObj) return;
            var builder = new UbNoteBuilderHelper(this, maniaObj);
            builder.RecomputeFromCurrentState();
        });
        
        refreshNoteIcons(selected);
    }

    private void refreshNoteIcons(IEnumerable<ManiaHitObject> hitObjects)
    {
        var targets = new HashSet<ManiaHitObject>(hitObjects);

        foreach (var stage in Playfield.Stages)
        {
            foreach (var column in stage.Columns)
            {
                foreach (var drawable in column.HitObjectContainer.AliveObjects)
                {
                    if (drawable.HitObject is ManiaHitObject mho && targets.Contains(mho) && drawable is DrawableNote note)
                        note.RefreshIcon();
                }
            }
        }
    }

    private void updateButtonStatesFromSelection()
    {
        var selected = EditorBeatmap.SelectedHitObjects.OfType<ManiaHitObject>().ToList();

        suppressSelectionFeedback = true;
        try
        {
            if (selected.Count == 0)
                return;

            // Cache inferred icons
            var iconsPerNote = selected.Select(h => new UbNoteBuilderHelper(this, h).InferObjectModifierIcons().ToHashSet()).ToList();

            updateButton(ModFlyingButton, UbIconType.ModFlying, iconsPerNote);
            updateButton(ModInvisibleButton, UbIconType.ModInvisible, iconsPerNote);
            updateButton(ModSwapImmediateButton, UbIconType.ModSwapImmediate, iconsPerNote);

            // Cop master button: true if any note has any cop sub-modifier
            bool allCop = iconsPerNote.All(set => set.Contains(UbIconType.ModCop1) || set.Contains(UbIconType.ModCop2) || set.Contains(UbIconType.ModCop3) || set.Contains(UbIconType.ModCop4));
            bool anyCop = iconsPerNote.Any(set => set.Contains(UbIconType.ModCop1) || set.Contains(UbIconType.ModCop2) || set.Contains(UbIconType.ModCop3) || set.Contains(UbIconType.ModCop4));
            ModCopButton.Current.Value = allCop ? TernaryState.True : (anyCop ? TernaryState.Indeterminate : TernaryState.False);

            updateButton(ModCop1Button, UbIconType.ModCop1, iconsPerNote);
            updateButton(ModCop2Button, UbIconType.ModCop2, iconsPerNote);
            updateButton(ModCop3Button, UbIconType.ModCop3, iconsPerNote);
            updateButton(ModCop4Button, UbIconType.ModCop4, iconsPerNote);
            updateButton(ModCopFinishButton, UbIconType.ModCopFinish, iconsPerNote);
            updateButton(ModCopHeavyButton, UbIconType.ModCopHeavy, iconsPerNote);
        }
        finally
        {
            suppressSelectionFeedback = false;
        }
    }

    private static void updateButton(DrawableTernaryButton button, UbIconType icon, List<HashSet<UbIconType>> iconsPerNote)
    {
        bool all = iconsPerNote.All(set => set.Contains(icon));
        bool any = iconsPerNote.Any(set => set.Contains(icon));
        button.Current.Value = all ? TernaryState.True : (any ? TernaryState.Indeterminate : TernaryState.False);
    }

    protected override void Update()
    {
        base.Update();

        if (BlueprintContainer.CurrentTool != null)
        {
            // Make buttons update
            var tool = BlueprintContainer.CurrentTool;

            string currentToolName = tool.Name;

            // When a note is selected, also reveal the modifier buttons that
            // apply to the selected note's tool so they can be edited.
            var selection = classifySelection();
            var selectedToolNames = selection.ToolNames;
            selectionContainsCop = selection.HasCop;

            foreach (var mapping in ModButtonToolMap)
            {
                var button = mapping.Button;
                var tools = mapping.ApplicableTools;
                var predicate = mapping.AvailabilityPredicate;

                bool predicateResult = predicate?.Invoke() ?? true;

                // With a selection, a button is only revealed if it applies to EVERY selected notes tool
                // Without a selection, fall back to the current placement tool.
                bool appliesToSelection = selectedToolNames.Count > 0 && selectedToolNames.All(tools.Contains);
                bool appliesToTool = selectedToolNames.Count == 0 && (tools.Contains(currentToolName) || KeyBasedCharting.Value);
                bool shouldEnable = (appliesToSelection || appliesToTool) && predicateResult;

                // Cop conversion is only valid when every selected note can be a cop note.
                if (!selection.AllCopCapable && button == ModCopButton)
                    shouldEnable = false;

                // Mixed cop/non-cop (Note/Hold) selection: only the cop master toggle is meaningful,
                // since non-cop modifiers are invalid on cop notes and cop sub-modifiers shouldn't yet
                // apply to the regular notes. Toggling the master converts the whole selection to/from cop.
                if (selection.MixedCop)
                    shouldEnable = button == ModCopButton;

                if (button.Enabled.Value != shouldEnable)
                {
                    button.Enabled.Value = shouldEnable;
                }
                button.Alpha = shouldEnable ? 1 : 0;
            }


            // Column hints
            var stage = Playfield.Stages[0];

            var columns = stage.Columns;

            var toolColumns = new List<int>();

            if (tool is UbNoteCompositionTool ubNTool)
            {
                toolColumns = ubNTool.Columns;
            }
            else if (tool is UbHoldNoteCompositionTool ubHTool)
            {
                toolColumns = ubHTool.Columns;
            }

            if (tool is UbNoteCompositionTool or UbHoldNoteCompositionTool &&
                SettingShowAllowedColumns.Value == TernaryState.True)
            {
                foreach (var col in columns)
                {
                    if (!toolColumns.Contains(col.Index))
                    {
                        col.FlashColour(Colour4.Gray.Lighten(0.35f), 200);
                    }
                    else
                    {
                        col.FlashColour(Colour4.White, 200);
                    }
                }
            }
            else
            {
                foreach (var col in columns)
                {
                    col.FlashColour(Colour4.White, 200);
                }
            }
        }
    }

    protected override void UpdateBeatSnapGrid()
    {
        base.UpdateBeatSnapGrid();

        // When key-charting is active with the Select tool and no selection,
        // show the grid across the entire visible column range.
        if (KeyBasedCharting.Value
            && BlueprintContainer.CurrentTool is SelectTool
            && !EditorBeatmap.SelectedHitObjects.Any()
            && BeatSnapGrid != null)
        {
            double currentTime = EditorClock.CurrentTime;
            double visibleTime = ScrollingInfo.TimeRange.Value;
            BeatSnapGrid.SelectionTimeRange = (currentTime - visibleTime, currentTime + visibleTime);
        }
    }

    protected override bool OnKeyDown(KeyDownEvent e)
    {
        if (!e.ControlPressed && !e.AltPressed && !e.SuperPressed)
        {
            if (keyBasedChartingHandler != null && keyBasedChartingHandler.TryPlaceNote(e.Key, e.ShiftPressed))
                return true;
        }

        return base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyUpEvent e)
    {
        keyBasedChartingHandler?.TryReleaseKey(e.Key);
    }

    protected override bool OnScroll(ScrollEvent e)
    {
        double scrollDelta = e.ScrollDelta.Y != 0 ? e.ScrollDelta.Y : e.ScrollDelta.X;

        if (keyBasedChartingHandler != null && keyBasedChartingHandler.TryAdjustHold(scrollDelta, e.ShiftPressed))
            return true;

        return base.OnScroll(e);
    }

    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);

        if (KeyBasedCharting.Value)
            EditorKeyBasedCharting.IsActive = false;

        if (hasTimingHandler != null)
            EditorBeatmap.HasTiming.ValueChanged -= hasTimingHandler;

        if (selectionChangedHandler != null)
            EditorBeatmap.SelectedHitObjects.CollectionChanged -= selectionChangedHandler;
    }
}

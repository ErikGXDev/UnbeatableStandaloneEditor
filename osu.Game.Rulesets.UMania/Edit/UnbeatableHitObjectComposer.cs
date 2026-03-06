using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Audio;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.UMania.Edit.Composition;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit;

[Cached]
public partial class UnbeatableHitObjectComposer : ManiaHitObjectComposer
{
    public UnbeatableHitObjectComposer(Ruleset ruleset)
        : base(ruleset)
    {
    }

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

    /*private readonly Bindable<TernaryState> modFlyingNote = new Bindable<TernaryState>();
    private readonly Bindable<TernaryState> modInvisibleNote = new Bindable<TernaryState>();
    private readonly Bindable<TernaryState> modSwapImmediate = new Bindable<TernaryState>();

    private readonly Bindable<TernaryState> modCopNote = new Bindable<TernaryState>();
    private readonly Bindable<TernaryState> modCopFinish = new Bindable<TernaryState>();
    private readonly Bindable<TernaryState> modCop1 = new Bindable<TernaryState>();
    private readonly Bindable<TernaryState> modCop2 = new Bindable<TernaryState>();
    private readonly Bindable<TernaryState> modCop3 = new Bindable<TernaryState>();
    private readonly Bindable<TernaryState> modCop4 = new Bindable<TernaryState>();
    private readonly Bindable<TernaryState> modCopHeavy = new Bindable<TernaryState>();
    private readonly Bindable<TernaryState> modCopImpossible = new Bindable<TernaryState>();*/


    public DrawableTernaryButton ModFlyingButton = null!;
    public DrawableTernaryButton ModInvisibleButton = null!;

    //public

    public DrawableTernaryButton ModCopButton = null!;
    public DrawableTernaryButton ModCopFinishButton = null!;
    public DrawableTernaryButton ModCop1Button = null!;
    public DrawableTernaryButton ModCop2Button = null!;
    public DrawableTernaryButton ModCop3Button = null!;
    public DrawableTernaryButton ModCop4Button = null!;
    public DrawableTernaryButton ModCopHeavyButton = null!;
    //public DrawableTernaryButton ModCopImpossibleButton = null!;


    public DrawableTernaryButton ModSwapImmediateButton = null!;



    // Create a dictionary that maps each button to a list of tool names that should have it enabled
    public List<ModMapping> ModButtonToolMap;

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
            //ModCopImpossibleButton = makeButton(modCopImpossible, "Impossible Cop", FontAwesome.Solid.UserShield),

        };

        //return base.CreateTernaryButtons();
    }

    private DrawableTernaryButton makeButton(string description, IconUsage icon)
    {
        return new DrawableTernaryButton
        {
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


    [BackgroundDependencyLoader]
    private void load()
    {
        ModButtonToolMap = new List<ModMapping>()
        {
            makeMapping(ModFlyingButton, ["Note", "Hold", "Dodge", "Double", "Spam", "Freestyle"], () => !isCopModding() ),
            makeMapping(ModInvisibleButton, ["Note", "Hold", "Double", "Spam", "Freestyle"], () => !isCopModding() ),
            makeMapping(ModSwapImmediateButton, ["Flip"], () => !isCopModding() ),
            makeMapping(ModCopButton, ["Note", "Hold"] ),
            makeMapping(ModCop1Button, ["Note", "Hold"], isCopModding ),
            makeMapping(ModCop2Button, ["Note", "Hold"], isCopModding ),
            makeMapping(ModCop3Button, ["Note", "Hold"], isCopModding ),
            makeMapping(ModCop4Button, ["Note", "Hold"], isCopModding ),
            makeMapping(ModCopFinishButton, ["Note", "Hold"], isCopModding ),
            makeMapping(ModCopHeavyButton, ["Note", "Hold"], isCopModding ),
            //{ ModCopImpossibleButton, new List<string> { "Note", "Hold" } },

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
                    }
                ]
            },
        });
    }

    protected override void Update()
    {
        base.Update();

        if (BlueprintContainer.CurrentTool != null)
        {
            // Make buttons update
            var tool = BlueprintContainer.CurrentTool;

            string currentToolName = tool.Name;

            foreach (var mapping in ModButtonToolMap)
            {
                var button = mapping.Button;
                var tools = mapping.ApplicableTools;
                var predicate = mapping.AvailabilityPredicate;

                bool predicateResult = predicate?.Invoke() ?? true;

                bool shouldEnable = tools.Contains(currentToolName) && predicateResult;

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
                        //col.Colour = Colour4.Gray;
                        col.FlashColour(Colour4.Gray.Lighten(0.35f), 200);
                    }
                    else
                    {
                        //col.Colour = Colour4.White;
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
}

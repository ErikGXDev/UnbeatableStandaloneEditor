using System;
using System.Collections.Generic;
using osu.Game.Screens.Edit.Components.TernaryButtons;

namespace osu.Game.Rulesets.UMania;

public class ModMapping
{
    public DrawableTernaryButton Button { get; set; }
    public List<string> ApplicableTools { get; set; }
    public Func<bool>? AvailabilityPredicate { get; set; }
}

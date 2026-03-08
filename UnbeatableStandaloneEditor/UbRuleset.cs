using osu.Game.Rulesets;
using osu.Game.Rulesets.UMania;

namespace UnbeatableStandaloneEditor;

public class UbRuleset
{
    public static RulesetInfo GetRulesetInfo()
    {
        var ruleset = new UManiaRuleset();

        return ruleset.RulesetInfo;
    }
}

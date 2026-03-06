using HarmonyLib;

namespace osu.Game.Rulesets.UMania.Patches;

//[HarmonyPatch(typeof(RulesetInfo), "OnlineID", MethodType.Getter)]
public class OnlinePatch
{
    public static bool Prefix(ref int __result, RulesetInfo __instance)
    {
        if (__instance.ShortName == "umania")
        {
            __result = 5;
            return false;
        }

        return true;
    }
}

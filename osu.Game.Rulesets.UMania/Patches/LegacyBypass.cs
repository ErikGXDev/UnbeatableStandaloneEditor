using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.UMania.Patches;

[HarmonyPatch(typeof(LegacyBeatmapEncoder), MethodType.Constructor, new[] { typeof(IBeatmap), typeof(ISkin) })]
public class LegacyBypass
{
    public static void Prefix(IBeatmap beatmap, ISkin? skin)
    {
        if (beatmap.BeatmapInfo.Ruleset.ShortName == "umania")
        {
            beatmap.BeatmapInfo.Ruleset.OnlineID = 3;
        }
    }

    public static void Postfix(IBeatmap beatmap, ISkin? skin)
    {
        if (beatmap.BeatmapInfo.Ruleset.ShortName == "umania")
        {
            beatmap.BeatmapInfo.Ruleset.OnlineID = 5;
        }
    }
}

[HarmonyPatch(typeof(LegacyBeatmapEncoder), "handleGeneral", new[] { typeof(TextWriter) })]
public class EncoderPatch
{
    public static void Prefix(LegacyBeatmapEncoder __instance, TextWriter writer)
    {
        var traverse = Traverse.Create(__instance);
    }

    public static void SetOnlineRulesetID(LegacyBeatmapDecoder instance, int id)
    {
        var traverse = Traverse.Create(instance);

        traverse.Field("onlineRulesetID").SetValue(id);
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var found = false;
        foreach (var instruction in instructions)
        {
            if (!found && instruction.opcode == OpCodes.Ldstr && instruction.operand is string str &&
                str == "Mode: {0}")
            {
                found = true;
                yield return new CodeInstruction(OpCodes.Ldstr, "Mode: 5");
            }
            else
            {
                yield return instruction;
            }
        }
    }
}

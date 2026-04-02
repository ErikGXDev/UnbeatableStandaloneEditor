using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.IO.Archives;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.UMania;
using osu.Game.Rulesets.UMania.Beatmaps;
using osu.Game.Rulesets.UMania.Edit.Blueprints;

namespace UnbeatableStandaloneEditor.Import;

public static class BeatmapImporter
{


    // Simply take in a stream, re-encode the beatmap, and put it in a new stream
    public static Stream ModifyBeatmap(Stream stream)
    {
        Beatmap? beatmap;
        using (var reader = new LineBufferedReader(stream))
        {
            var decoder = Decoder.GetDecoder<Beatmap>(reader);
            beatmap = decoder.Decode(reader);
        }

        if (beatmap == null)
        {
            Logger.Log($"Failed to decode beatmap from stream");
            return stream;
        }

        beatmap.BeatmapInfo.Ruleset = UbRuleset.GetRulesetInfo();

        var converter = new ManiaBeatmapConverter(beatmap, new UManiaRuleset());

        var converted = converter.Convert();

        reverseExportTransformations(converted);

        var encoder = new LegacyBeatmapEncoder(converted, null);
        var outputStream = new MemoryStream();
        using (var textWriter = new StreamWriter(outputStream, leaveOpen: true))
        {
            encoder.Encode(textWriter);
            textWriter.Flush();
        }
        outputStream.Position = 0;

        return outputStream;
    }

    // osu!lazer exports beatmaps with X 512, unbeatable only recognizes 511
    private static void reverseExportTransformations(IBeatmap beatmap)
    {
        bool isUMania = beatmap.BeatmapInfo.Ruleset.OnlineID == 5 || beatmap.BeatmapInfo.Ruleset.OnlineID == 3;

        foreach (var hitObject in beatmap.HitObjects)
        {
            reverseSampleBanks(hitObject);

            if (isUMania && hitObject is IHasPosition positionedObject)
            {
                if (positionedObject.X == 511)
                {
                    positionedObject.X = 512;
                    //Logger.Log($"Converted position 511 to 512 for hitobject at {hitObject.StartTime}ms");
                }
            }
        }

        //Logger.Log($"Reversed export transformations for {beatmap.HitObjects.Count} hitobjects");
    }

    private static void reverseSampleBanks(HitObject hitObject)
    {
        if (hitObject.Samples == null || hitObject.Samples.Count == 0)
            return;

        var noteBuilder = new UbNoteBuilder(hitObject);

        //Logger.Log("Main bank: " + noteBuilder.GetMainSample().Bank + " | " + noteBuilder.GetAdditionSample().Bank);

        var mainBank = noteBuilder.GetMainSample().Bank;

        var corrected = fromLegacySampleBank(mainBank);

        if (mainBank != corrected)
        {
            noteBuilder.ApplyMainBank(corrected);
            //Logger.Log($"-> Converted main bank from {mainBank} to {corrected}");
        }


    }

    private static string fromLegacySampleBank(string? bank)
    {
        return bank?.ToLowerInvariant() switch
        {
            HitSampleInfo.BANK_NORMAL => HitSampleInfo.BANK_SOFT,
            HitSampleInfo.BANK_DRUM => HitSampleInfo.BANK_DRUM,
            HitSampleInfo.BANK_SOFT => HitSampleInfo.BANK_SOFT,
            _ => HitSampleInfo.BANK_NORMAL,
        };
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.UMania.Edit.Setup
{
    public partial class UbBeatmapEncoder : UbLegacyBeatmapEncoder
    {
        public UbBeatmapEncoder(IBeatmap beatmap, ISkin? skin)
            : base(beatmap, skin)
        {

               
        }


        

        public void EncodeB(TextWriter writer)
        {
            var tempWriter = new StringWriter();
            tempWriter.NewLine = writer.NewLine;
            
            writer.WriteLine("// Created with Erik's Standalone Editor");
            writer.WriteLine("// https://github.com/ErikGXDev/UnbeatableStandaloneEditor");
            writer.WriteLine("");
            
           
            Encode(tempWriter);

            string output = tempWriter.ToString();

            // Replace all beginning 512's with 511


            string[] splitLines = output.Split(writer.NewLine);
            foreach (string line in splitLines.ToList())
            {
                if (line.StartsWith("512,"))
                {
                    string newLine = line.Replace("512,", "511,");
                    writer.WriteLine(newLine);
                }
                else
                {
                    writer.WriteLine(line);
                }
            }
        }


    }
}

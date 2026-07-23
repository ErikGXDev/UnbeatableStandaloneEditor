using osu.Framework.Extensions;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.IO.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using SixLabors.ImageSharp.Memory;

namespace UnbeatableStandaloneEditor.Import;

public class SingleFileArchiveReader : ArchiveReader
{


    List<string> files;

    Dictionary<string, string> baseNameToFullPath = new Dictionary<string, string>();

    public SingleFileArchiveReader(List<string> files) : base("zippy")
    {
        this.files = files;

        foreach (var entry in files.ToList())
        {
            if (entry.EndsWith(".txt") || entry.EndsWith(".osu"))
            {
                findBeatmapAudio(entry);
            }
        }
    }

    private void findBeatmapAudio(string path)
    {
        var decoder = new LegacyBeatmapDecoder();

        using var stream = File.OpenRead(path);

        using var lineBufferedReader = new LineBufferedReader(stream);

        var beatmap = decoder.Decode(lineBufferedReader);

        var audio = beatmap.Metadata.AudioFile;

        var audioPath = Path.Combine(Path.GetDirectoryName(path) ?? "", audio);

        if (File.Exists(audioPath))
        {
            files.Add(audioPath);
        }
    }


    public override IEnumerable<string> Filenames
    {
        get
        {
            var list = new List<string>();
            foreach (var entry in files)
            {
                if (Directory.Exists(entry))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(entry))
                {
                    continue;
                }

                var baseName = Path.GetFileName(entry);

                if (entry.EndsWith(".txt"))
                {
                    baseName += ".osu";
                }

                list.Add(baseName);


                baseNameToFullPath[baseName] = entry;
            }

            list.ExcludeSystemFileNames();

            return list;
        }
    }

    public override Stream GetStream(string name)
    {
        /*var entryName = name;*/
        var isBeatmap = false;

        if (name.EndsWith(".txt") || name.EndsWith(".osu"))
        {
            isBeatmap = true;
        }
        /*if (name.EndsWith(".txt.osu"))
        {
            isBeatmap = true;
            entryName = name.Substring(0, name.Length - 4);
        }*/

        // Log all of baseNameToFullPath
        Logger.Log("baseNameToFullPath: " + string.Join(", ", baseNameToFullPath.Select(kvp => kvp.Key + " -> " + kvp.Value)));

        Logger.Log("Getting stream for " + name + " - " + baseNameToFullPath.GetValueOrDefault(name));

        var entry = baseNameToFullPath.GetValueOrDefault(name);
        if (entry == null) return null;

        using var stream = File.OpenRead(entry);

        var memoryStream = new MemoryStream(stream.ReadAllRemainingBytesToArray());

        if (!isBeatmap) return memoryStream;

        var beatmapStream = BeatmapImporter.ModifyBeatmap(memoryStream);

        return beatmapStream;


    }

    public override void Dispose()
    {

    }
}

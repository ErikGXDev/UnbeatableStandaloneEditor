using osu.Framework.Extensions;
using osu.Framework.IO.Stores;
using osu.Game.IO.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using SixLabors.ImageSharp.Memory;

namespace UnbeatableStandaloneEditor.Import;

public class ProxyArchiveReader : ArchiveReader
{


    private ZipArchive archive;

    public ProxyArchiveReader(string filePath) : base("zippy")
    {
        var zipStream = File.OpenRead(filePath);

        archive = ZipArchive.Open(zipStream, new ReaderOptions
        {
            ArchiveEncoding = ZipArchiveReader.DEFAULT_ENCODING
        });
    }



    public override IEnumerable<string> Filenames
    {
        get
        {
            var list = new List<string>();
            foreach (var entry in archive.Entries)
            {
                if (entry.IsDirectory)
                {
                    continue;
                }

                if (entry.Key == null)
                {
                    continue;
                }

                if (entry.Key.EndsWith(".txt"))
                {
                    list.Add(entry.Key + ".osu");
                }
                else
                {
                    list.Add(entry.Key);
                }
            }

            list.ExcludeSystemFileNames();

            return list;
        }
    }

    public override Stream GetStream(string name)
    {
        var entryName = name;
        var isBeatmap = false;
        if (name.EndsWith(".txt.osu"))
        {
            isBeatmap = true;
            entryName = name.Substring(0, name.Length - 4);
        }

        var entry = archive.Entries.SingleOrDefault(e => e.Key == entryName);
        if (entry == null) return null;

        using var stream = entry.OpenEntryStream();

        var memoryStream = new MemoryStream(stream.ReadAllRemainingBytesToArray());

        if (!isBeatmap) return memoryStream;

        var beatmapStream = BeatmapImporter.ModifyBeatmap(memoryStream);

        return beatmapStream;


    }

    public override void Dispose()
    {
        archive.Dispose();
    }
}

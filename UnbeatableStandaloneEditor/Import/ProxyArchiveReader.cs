using osu.Framework.Extensions;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Game.IO.Archives;
using osu.Game.Overlays.Settings.Sections.Audio;
using osu.Game.Utils;
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
                    // Optional offset test
                    if (BeatmapImporter.ShouldTestOffset)
                    {
                        if (SupportedExtensions.AUDIO_EXTENSIONS.Contains(Path.GetExtension(entry.Key).ToLowerInvariant()))
                        {
                            // Extract entry to a temp file
                            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(entry.Key));
                            using (var entryStream = entry.OpenEntryStream())
                            using (var tempFileStream = File.Create(tempFilePath))
                            {
                                entryStream.CopyTo(tempFileStream);
                            }

                            BeatmapImporter.offsetMs = ImportOffsetTester.GetOffset(tempFilePath);

                            Logger.Log($"Offset for {entry.Key}: {BeatmapImporter.offsetMs} ms");

                            File.Delete(tempFilePath);
                        }
                    }

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
            entryName = name.Substring(0, name.Length - 4);
        }

        if (name.EndsWith(".txt") || name.EndsWith(".osu"))
        {
            isBeatmap = true;
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

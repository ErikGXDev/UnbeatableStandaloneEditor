using System.Diagnostics;
using System.IO.Compression;

namespace UnbeatableStandaloneEditor.Update;

public class Updater
{
    public static string BaseDownloadUrl =
        "https://github.com/ErikGXDev/UnbeatableStandaloneEditor/releases/latest/download/unbeatable-editor-";

    private static readonly HttpClient HttpClient = new();


    // Download the new release, start it and close the current application
    public static async Task<bool> FullDownload(IProgress<double>? progress = null)
    {
        bool downloadSuccess = await DownloadLatestRelease(progress);

        if (downloadSuccess)
        {
            string targetDirectory = SafelyGetTargetDirectory();
            string executablePath = Path.Combine(targetDirectory, GetExecutableName());

            if (!File.Exists(executablePath))
            {
                throw new FileNotFoundException($"Executable not found at {executablePath}");
            }

            int currentProcessId = Environment.ProcessId;
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                ArgumentList = { "--updated1", currentProcessId.ToString() },
                UseShellExecute = true
            });

            Environment.Exit(0);
        }

        return downloadSuccess;
    }


    private static async Task<bool> DownloadLatestRelease(IProgress<double>? progress = null)
    {
        string systemString = getSystemString();
        string downloadUrl = BaseDownloadUrl + systemString + ".zip";
        string targetDirectory = SafelyGetTargetDirectory();

        CleanUpTargetDirectory();
        Directory.CreateDirectory(targetDirectory);

        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("UnbeatableStandaloneEditor");

        var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var canReportProgress = totalBytes > 0 && progress != null;

        using (var stream = await response.Content.ReadAsStreamAsync())
        {
            if (canReportProgress)
            {
                // Wrap the stream to report download progress
                var progressStream = new ProgressStream(stream, totalBytes, progress!);
                await Task.Run(() =>
                {
                    using (var archive = new ZipArchive(progressStream))
                    {
                        archive.ExtractToDirectory(targetDirectory, true);
                    }
                });
            }
            else
            {
                // Fallback without progress reporting
                await Task.Run(() =>
                {
                    using (var archive = new ZipArchive(stream))
                    {
                        archive.ExtractToDirectory(targetDirectory, true);
                    }
                });
            }
        }

        return true;
    }

    // Stream wrapper that can report progress
    private sealed class ProgressStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _totalBytes;
        private readonly IProgress<double> _progress;
        private long _bytesRead;

        public ProgressStream(Stream innerStream, long totalBytes, IProgress<double> progress)
        {
            _innerStream = innerStream;
            _totalBytes = totalBytes;
            _progress = progress;
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _innerStream.Read(buffer, offset, count);
            _bytesRead += read;
            _progress.Report((double)_bytesRead / _totalBytes);
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = await _innerStream.ReadAsync(buffer, cancellationToken);
            _bytesRead += read;
            _progress.Report((double)_bytesRead / _totalBytes);
            return read;
        }

        public override void Flush() => _innerStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
        protected override void Dispose(bool disposing)
        {
            if (disposing) _innerStream.Dispose();
            base.Dispose(disposing);
        }
    }


    public static string SafelyGetTargetDirectory()
    {
        string appDirectory = AppContext.BaseDirectory;
        string targetDirectory = Path.Combine(appDirectory, "__download");

        if (!targetDirectory.StartsWith(appDirectory))
        {
            throw new InvalidOperationException();
        }

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        return targetDirectory;
    }

    public static void CleanUpTargetDirectory()
    {
        string targetDirectory = SafelyGetTargetDirectory();

        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, true);
        }
    }


    private static string getSystemString()
    {
        if (OperatingSystem.IsWindows())
        {
            return "windows-x64";
        }
        else if (OperatingSystem.IsLinux())
        {
            return "linux-x64";
        }
        else if (OperatingSystem.IsMacOS())
        {
            return "macos-x64";
        }
        else
        {
            throw new Exception("Unsupported operating system");
        }
    }

    public static string GetExecutableName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "UnbeatableStandaloneEditor.exe";
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return "UnbeatableStandaloneEditor";
        }
        else
        {
            throw new Exception("Unsupported operating system");
        }
    }
}

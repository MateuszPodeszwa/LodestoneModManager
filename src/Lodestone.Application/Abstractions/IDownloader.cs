using Lodestone.Domain.Common;

namespace Lodestone.Application.Abstractions;

/// <summary>Progress of a byte transfer. <see cref="Fraction"/> is null when the total size is unknown.</summary>
public sealed record TransferProgress(long BytesTransferred, long? TotalBytes)
{
    public double? Fraction => TotalBytes is > 0 ? (double)BytesTransferred / TotalBytes.Value : null;
}

/// <summary>What to download: the source URL, the file name to save it as, and an optional expected
/// SHA-512 the result is verified against.</summary>
public sealed record DownloadRequest(string Url, string FileName, string? ExpectedSha512 = null);

/// <summary>A completed download: the temp-file path, its size in bytes and the SHA-512 actually computed.</summary>
public sealed record DownloadedFile(string Path, long SizeBytes, string Sha512);

/// <summary>
/// Downloads a file to a temporary location, verifying its SHA-512 when supplied. Concurrency is
/// bounded internally by the "concurrent downloads" setting.
/// </summary>
public interface IDownloader
{
    Task<Result<DownloadedFile>> DownloadAsync(
        DownloadRequest request,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default);
}

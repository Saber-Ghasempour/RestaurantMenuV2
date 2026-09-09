using System.Buffers.Binary;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using RestaurantMenu.Media.Application.Abstractions;

namespace RestaurantMenu.Media.Infrastructure.Storage;

public sealed record ObjectStorageOptions(string BucketName, string ServiceUrl, string AccessKey,
    string SecretKey, bool ForcePathStyle = true, string? PublicServiceUrl = null);

public sealed class S3ObjectStorage(IAmazonS3 client, ObjectStorageOptions options) : IObjectStorage
{
    public Task<Uri> CreateUploadUrlAsync(string key, string contentType, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var signer = CreateSigningClient();
        var url = signer.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName, Key = key, Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(lifetime), ContentType = contentType
        });
        return Task.FromResult(new Uri(url));
    }

    public async Task<StoredObjectMetadata?> InspectAsync(string key, long maximumBytes, CancellationToken cancellationToken)
    {
        GetObjectResponse response;
        try { response = await client.GetObjectAsync(options.BucketName, key, cancellationToken); }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return null; }
        using (response)
        await using (var stream = response.ResponseStream)
        using (var buffer = new MemoryStream())
        {
            var chunk = new byte[81920];
            long total = 0;
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                total += read;
                if (total > maximumBytes) throw new InvalidDataException("Stored object exceeds the configured media limit.");
                await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
            }
            var bytes = buffer.ToArray();
            var detected = DetectImage(bytes);
            return new StoredObjectMetadata(detected.ContentType, total,
                Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), detected.Width, detected.Height);
        }
    }

    public Task<Uri> CreateReadUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var signer = CreateSigningClient();
        var url = signer.GetPreSignedURL(new GetPreSignedUrlRequest
        { BucketName = options.BucketName, Key = key, Verb = HttpVerb.GET, Expires = DateTime.UtcNow.Add(lifetime) });
        return Task.FromResult(new Uri(url));
    }

    private AmazonS3Client CreateSigningClient()
    {
        Amazon.AWSConfigsS3.UseSignatureVersion4 = true;
        return new AmazonS3Client(options.AccessKey, options.SecretKey, new AmazonS3Config
        {
            ServiceURL = options.PublicServiceUrl ?? options.ServiceUrl,
            ForcePathStyle = options.ForcePathStyle,
            AuthenticationRegion = "us-east-1",
            SignatureVersion = "4"
        });
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        client.DeleteObjectAsync(options.BucketName, key, cancellationToken);

    private static (string ContentType, int Width, int Height) DetectImage(byte[] b)
    {
        if (b.Length >= 24 && b.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}))
            return ("image/png", BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(20, 4)));
        if (b.Length >= 12 && b[0] == 0xff && b[1] == 0xd8)
        {
            var p = 2;
            while (p + 9 < b.Length)
            {
                if (b[p] != 0xff) { p++; continue; }
                var marker = b[p + 1];
                if (marker is >= 0xc0 and <= 0xc3)
                    return ("image/jpeg", BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(p + 7, 2)), BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(p + 5, 2)));
                if (p + 4 > b.Length) break;
                var length = BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(p + 2, 2));
                if (length < 2) break;
                p += 2 + length;
            }
        }
        if (b.Length >= 30 && b.AsSpan(0, 4).SequenceEqual("RIFF"u8) && b.AsSpan(8, 4).SequenceEqual("WEBP"u8) && b.AsSpan(12, 4).SequenceEqual("VP8X"u8))
        {
            var width = 1 + b[24] + (b[25] << 8) + (b[26] << 16);
            var height = 1 + b[27] + (b[28] << 8) + (b[29] << 16);
            return ("image/webp", width, height);
        }
        throw new InvalidDataException("Stored object is not a supported image.");
    }
}

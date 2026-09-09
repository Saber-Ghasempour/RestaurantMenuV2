using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Media.Application.Abstractions;
using RestaurantMenu.Media.Domain.Assets;
using RestaurantMenu.Media.Infrastructure.Database;
using RestaurantMenu.Media.Infrastructure.Storage;
using Testcontainers.PostgreSql;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace RestaurantMenu.Media.IntegrationTests;

public sealed class MediaInfrastructureTests : IAsyncLifetime
{
    private const string AccessKey = "minioadmin";
    private const string SecretKey = "minioadmin";
    private const string Bucket = "restaurant-menu-media";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine").Build();
    private readonly IContainer _minio = new ContainerBuilder("minio/minio:RELEASE.2025-09-07T16-13-09Z")
        .WithEnvironment("MINIO_ROOT_USER", AccessKey).WithEnvironment("MINIO_ROOT_PASSWORD", SecretKey)
        .WithCommand("server", "/data").WithPortBinding(9000, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(9000).ForPath("/minio/health/ready")))
        .Build();

    public Task InitializeAsync() => Task.WhenAll(_postgres.StartAsync(), _minio.StartAsync());
    public async Task DisposeAsync() { await _postgres.DisposeAsync(); await _minio.DisposeAsync(); }

    [Fact]
    public async Task PostgreSqlShouldPersistLifecycleAndEnforceTenantChecksumUniqueness()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>().UseNpgsql(_postgres.GetConnectionString(),
            o => o.MigrationsHistoryTable("__ef_migrations_history", "media")).Options;
        await using var db = new MediaDbContext(options);
        await db.Database.MigrateAsync();
        var tenant = Guid.CreateVersion7();
        db.MediaAssets.Add(Create(tenant, "one"));
        await db.SaveChangesAsync();
        db.MediaAssets.Add(Create(tenant, "two"));
        await Assert.ThrowsAsync<DuplicateMediaChecksumException>(() => db.SaveChangesAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task S3AdapterShouldInspectActualPngAndIssueSignedUrls()
    {
        var serviceUrl = $"http://{_minio.Hostname}:{_minio.GetMappedPublicPort(9000)}";
        using var client = new AmazonS3Client(AccessKey, SecretKey, new AmazonS3Config { ServiceURL = serviceUrl, ForcePathStyle = true });
        await client.PutBucketAsync(new PutBucketRequest { BucketName = Bucket });
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        await client.PutObjectAsync(new PutObjectRequest { BucketName = Bucket, Key = "test/image", InputStream = new MemoryStream(png), ContentType = "text/plain" });
        var storage = new S3ObjectStorage(client, new ObjectStorageOptions(Bucket, serviceUrl, AccessKey, SecretKey));

        var metadata = await storage.InspectAsync("test/image", MediaAsset.MaxSizeBytes, default);

        Assert.NotNull(metadata);
        Assert.Equal("image/png", metadata.ContentType);
        Assert.Equal(1, metadata.Width);
        Assert.Equal(1, metadata.Height);
        Assert.Contains("X-Amz-Signature", (await storage.CreateReadUrlAsync("test/image", TimeSpan.FromMinutes(1), default)).Query);
    }

    private static MediaAsset Create(Guid tenant, string suffix) => MediaAsset.Initiate(MediaAssetId.New(), tenant,
        $"restaurants/{tenant:N}/{suffix}", $"{suffix}.png", "image/png", 10,
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "actor", DateTimeOffset.UtcNow).Value;
}

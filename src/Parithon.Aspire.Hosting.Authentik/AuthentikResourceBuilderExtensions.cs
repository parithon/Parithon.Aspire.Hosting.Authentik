using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding and configuring <see cref="AuthentikResource"/> resources.
/// </summary>
public static class AuthentikResourceBuilderExtensions
{
    private const int DefaultHttpTargetPort = 9000;
    private const int DefaultHttpsTargetPort = 9443;
    private const string DefaultAdminEmail = "admin";
    private const string DefaultDataDirectory = "/var/lib/authentik";

    /// <summary>
    /// Adds an Authentik resource and auto-provisions a child Postgres database resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The resource name.</param>
    /// <param name="adminPassword">The bootstrap admin password parameter resource.</param>
    /// <param name="port">Optional HTTP port override. Defaults to 9000.</param>
    /// <param name="adminUsername">Optional bootstrap admin email parameter resource.</param>
    /// <returns>The Authentik resource builder.</returns>
    public static IResourceBuilder<AuthentikResource> AddAuthentik(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        IResourceBuilder<ParameterResource> adminPassword,
        int? port = null,
        IResourceBuilder<ParameterResource>? adminUsername = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(adminPassword);

        var postgresPassword = builder.AddParameter($"{name}-postgres-password", secret: true);
        var postgres = builder.AddPostgres($"{name}-postgres", password: postgresPassword);
        var postgresDatabase = postgres.AddDatabase($"{name}-db");
        var redis = builder.AddRedis($"{name}-redis");

        var authentikResource = new AuthentikResource(name);

        var authentik = builder.AddResource(authentikResource)
            .WithImage(AuthentikContainerImageTags.Image)
            .WithImageTag(AuthentikContainerImageTags.Tag)
            .WithImageRegistry(AuthentikContainerImageTags.Registry)
            .WithArgs("server")
            .WithHttpEndpoint(targetPort: DefaultHttpTargetPort, port: port, name: AuthentikResource.HttpEndpointName)
            .WithEndpoint(targetPort: DefaultHttpsTargetPort, name: AuthentikResource.HttpsEndpointName, scheme: "https")
            .WithEnvironment("AUTHENTIK_BOOTSTRAP_PASSWORD", adminPassword)
            .WithHttpHealthCheck(path: "/-/health/live/", endpointName: AuthentikResource.HttpEndpointName)
            .WaitFor(postgresDatabase)
            .WaitFor(redis);

        authentik.WithAnnotation(new AutoProvisionedPostgresAnnotation(postgres.Resource, postgresDatabase.Resource));
        authentik.WithAnnotation(new AutoProvisionedRedisAnnotation(redis.Resource));

        if (adminUsername is not null)
        {
            authentik.WithEnvironment("AUTHENTIK_BOOTSTRAP_EMAIL", adminUsername);
        }
        else
        {
            authentik.WithEnvironment("AUTHENTIK_BOOTSTRAP_EMAIL", DefaultAdminEmail);
        }

        EnsureSecretKey(authentik);
        ConfigureRedisEnvironment(authentik, redis);

        ConfigurePostgresEnvironment(authentik, postgresDatabase);

        return authentik;
    }

    /// <summary>
    /// Overrides Authentik Postgres wiring to use an existing Postgres database resource.
    /// </summary>
    /// <param name="builder">The Authentik builder.</param>
    /// <param name="postgresDb">The external Postgres database resource.</param>
    /// <returns>The updated Authentik resource builder.</returns>
    public static IResourceBuilder<AuthentikResource> WithReference(
        this IResourceBuilder<AuthentikResource> builder,
        IResourceBuilder<PostgresDatabaseResource> postgresDb)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(postgresDb);

        RemoveAutoProvisionedPostgresResources(builder);
        ConfigurePostgresEnvironment(builder, postgresDb);

        // Waiting for the database is sufficient; the database cannot become healthy
        // unless its parent Postgres server has already started.
        var configuredBuilder = builder.WaitFor(postgresDb);
        RemoveWaitAnnotationsForResource(configuredBuilder, postgresDb.Resource.Parent);

        return configuredBuilder;
    }

    /// <summary>
    /// Overrides Authentik Redis wiring to use an existing Redis resource.
    /// </summary>
    /// <param name="builder">The Authentik builder.</param>
    /// <param name="redis">The external Redis resource.</param>
    /// <returns>The updated Authentik resource builder.</returns>
    public static IResourceBuilder<AuthentikResource> WithReference(
        this IResourceBuilder<AuthentikResource> builder,
        IResourceBuilder<RedisResource> redis)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(redis);

        RemoveAutoProvisionedRedisResources(builder);
        ConfigureRedisEnvironment(builder, redis);

        return builder.WaitFor(redis);
    }

    /// <summary>
    /// Adds a persistent data volume mounted at Authentik's data path.
    /// </summary>
    /// <param name="builder">The Authentik builder.</param>
    /// <param name="name">Optional volume name. A generated name is used when omitted.</param>
    /// <returns>The updated Authentik resource builder.</returns>
    public static IResourceBuilder<AuthentikResource> WithDataVolume(
        this IResourceBuilder<AuthentikResource> builder,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var volumeName = string.IsNullOrWhiteSpace(name)
            ? VolumeNameGenerator.Generate(builder, "data")
            : name;

        return builder.WithVolume(volumeName, DefaultDataDirectory);
    }

    private static IResourceBuilder<AuthentikResource> EnsureSecretKey(
        IResourceBuilder<AuthentikResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var secretKeyParameter = builder.ApplicationBuilder.AddParameter(
            $"{builder.Resource.Name}-secret-key",
            () => GenerateAndLogSecretKey(builder.Resource.Name),
            publishValueAsDefault: false,
            secret: true);

        return builder.WithEnvironment("AUTHENTIK_SECRET_KEY", secretKeyParameter);
    }

    private static string GenerateAndLogSecretKey(string resourceName)
    {
        var bytes = RandomNumberGenerator.GetBytes(45);
        var secretKey = Convert.ToBase64String(bytes);
        Console.WriteLine($"[Authentik] Generated AUTHENTIK_SECRET_KEY for resource '{resourceName}': {secretKey}");
        return secretKey;
    }

    private static void ConfigurePostgresEnvironment(
        IResourceBuilder<AuthentikResource> authentik,
        IResourceBuilder<PostgresDatabaseResource> postgresDb)
    {
        var parent = postgresDb.Resource.Parent;

        authentik
            .WithEnvironment("AUTHENTIK_POSTGRESQL__HOST", parent.Host)
            .WithEnvironment("AUTHENTIK_POSTGRESQL__PORT", parent.Port)
            .WithEnvironment("AUTHENTIK_POSTGRESQL__USER", parent.UserNameReference)
            .WithEnvironment("AUTHENTIK_POSTGRESQL__PASSWORD", parent.PasswordParameter)
            .WithEnvironment("AUTHENTIK_POSTGRESQL__NAME", postgresDb.Resource.DatabaseName);
    }

    private static void ConfigureRedisEnvironment(
        IResourceBuilder<AuthentikResource> authentik,
        IResourceBuilder<RedisResource> redis)
    {
        var secondaryEndpoint = redis.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .FirstOrDefault(e => string.Equals(e.Name, "secondary", StringComparison.Ordinal));

        var host = redis.Resource.Host;
        var port = redis.Resource.Port;

        // Aspire Redis can expose a TLS primary endpoint plus a non-TLS secondary endpoint.
        // Authentik is configured against plain Redis host/port values, so prefer the non-TLS endpoint when present.
        if (secondaryEndpoint is not null)
        {
            var endpoint = redis.GetEndpoint("secondary");
            host = endpoint.Property(EndpointProperty.Host);
            port = endpoint.Property(EndpointProperty.Port);
        }

        authentik
            .WithEnvironment("AUTHENTIK_REDIS__HOST", host)
            .WithEnvironment("AUTHENTIK_REDIS__PORT", port);

        if (redis.Resource.PasswordParameter is not null)
        {
            authentik.WithEnvironment("AUTHENTIK_REDIS__PASSWORD", redis.Resource.PasswordParameter);
        }
    }

    private static void RemoveAutoProvisionedPostgresResources(IResourceBuilder<AuthentikResource> builder)
    {
        var autoProvisioned = builder.Resource.Annotations.OfType<AutoProvisionedPostgresAnnotation>().FirstOrDefault();
        if (autoProvisioned is null)
        {
            return;
        }

        foreach (var waitAnnotation in builder.Resource.Annotations.OfType<WaitAnnotation>()
                     .Where(w => ReferenceEquals(w.Resource, autoProvisioned.DatabaseResource) ||
                                 ReferenceEquals(w.Resource, autoProvisioned.ServerResource))
                     .ToList())
        {
            builder.Resource.Annotations.Remove(waitAnnotation);
        }

        if (builder.ApplicationBuilder.Resources is ICollection<IResource> resources)
        {
            var autoProvisionedPasswordParameterName = $"{builder.Resource.Name}-postgres-password";
            foreach (var parameterResource in resources.OfType<ParameterResource>()
                         .Where(p => string.Equals(p.Name, autoProvisionedPasswordParameterName, StringComparison.Ordinal))
                         .ToList())
            {
                resources.Remove(parameterResource);
            }

            resources.Remove(autoProvisioned.DatabaseResource);
            resources.Remove(autoProvisioned.ServerResource);
        }

        builder.Resource.Annotations.Remove(autoProvisioned);
    }

    private static void RemoveAutoProvisionedRedisResources(IResourceBuilder<AuthentikResource> builder)
    {
        var autoProvisioned = builder.Resource.Annotations.OfType<AutoProvisionedRedisAnnotation>().FirstOrDefault();
        if (autoProvisioned is null)
        {
            return;
        }

        foreach (var waitAnnotation in builder.Resource.Annotations.OfType<WaitAnnotation>()
                     .Where(w => ReferenceEquals(w.Resource, autoProvisioned.RedisResource))
                     .ToList())
        {
            builder.Resource.Annotations.Remove(waitAnnotation);
        }

        if (builder.ApplicationBuilder.Resources is ICollection<IResource> resources)
        {
            resources.Remove(autoProvisioned.RedisResource);
        }

        builder.Resource.Annotations.Remove(autoProvisioned);
    }

    private static void RemoveWaitAnnotationsForResource(
        IResourceBuilder<AuthentikResource> builder,
        IResource resource)
    {
        foreach (var waitAnnotation in builder.Resource.Annotations.OfType<WaitAnnotation>()
                     .Where(w => ReferenceEquals(w.Resource, resource))
                     .ToList())
        {
            builder.Resource.Annotations.Remove(waitAnnotation);
        }
    }

    private sealed class AutoProvisionedPostgresAnnotation(
        PostgresServerResource serverResource,
        PostgresDatabaseResource databaseResource) : IResourceAnnotation
    {
        public PostgresServerResource ServerResource { get; } = serverResource;

        public PostgresDatabaseResource DatabaseResource { get; } = databaseResource;
    }

    private sealed class AutoProvisionedRedisAnnotation(
        RedisResource redisResource) : IResourceAnnotation
    {
        public RedisResource RedisResource { get; } = redisResource;
    }
}

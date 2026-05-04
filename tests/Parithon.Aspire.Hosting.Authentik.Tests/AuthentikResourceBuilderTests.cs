using System.Linq;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Parithon.Aspire.Hosting.Authentik.Tests;

public class AuthentikResourceBuilderTests
{
    [Fact]
    public void AddAuthentik_ConfiguresExpectedEndpoints_AndWaitDependency()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var password = builder.AddParameter("admin-password", secret: true);

        var authentik = builder.AddAuthentik("authentik", password);

        var endpointAnnotations = authentik.Resource.Annotations.OfType<EndpointAnnotation>().ToList();
        Assert.Contains(endpointAnnotations, e => e.Name == "http" && e.TargetPort == 9000);
        Assert.Contains(endpointAnnotations, e => e.Name == "https" && e.TargetPort == 9443);

        var waitAnnotations = authentik.Resource.Annotations.OfType<WaitAnnotation>().ToList();
        Assert.NotEmpty(waitAnnotations);
        Assert.Contains(waitAnnotations, w => w.Resource.Name == "authentik-redis");
    }

    [Fact]
    public void WithReference_AddsWaitDependencyForExternalDatabase()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var password = builder.AddParameter("admin-password", secret: true);

        var postgres = builder.AddPostgres("postgres");
        var postgresDb = postgres.AddDatabase("authdb");

        var authentik = builder.AddAuthentik("authentik", password)
            .WithReference(postgresDb);

        var waitAnnotations = authentik.Resource.Annotations.OfType<WaitAnnotation>().ToList();
        Assert.Contains(waitAnnotations, w => ReferenceEquals(w.Resource, postgresDb.Resource));
        Assert.DoesNotContain(waitAnnotations, w => ReferenceEquals(w.Resource, postgres.Resource));
    }

    [Fact]
    public void WithReference_AddsWaitDependencyForExternalRedis()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var password = builder.AddParameter("admin-password", secret: true);

        var externalRedis = builder.AddRedis("external-redis");

        var authentik = builder.AddAuthentik("authentik", password)
            .WithReference(externalRedis);

        var waitAnnotations = authentik.Resource.Annotations.OfType<WaitAnnotation>().ToList();
        Assert.Contains(waitAnnotations, w => ReferenceEquals(w.Resource, externalRedis.Resource));
    }

    [Fact]
    public void WithReference_RemovesAutoProvisionedPostgresResources()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var password = builder.AddParameter("admin-password", secret: true);

        var externalPostgres = builder.AddPostgres("external-postgres");
        var externalPostgresDb = externalPostgres.AddDatabase("external-db");

        _ = builder.AddAuthentik("authentik", password)
            .WithReference(externalPostgresDb);

        var resources = builder.Resources.ToList();
        Assert.DoesNotContain(resources, r => r.Name == "authentik-postgres");
        Assert.DoesNotContain(resources, r => r.Name == "authentik-db");
        Assert.Contains(resources, r => r.Name == "external-postgres");
        Assert.Contains(resources, r => r.Name == "external-db");
    }

    [Fact]
    public void WithReference_RemovesAutoProvisionedRedisResource()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var password = builder.AddParameter("admin-password", secret: true);

        var externalRedis = builder.AddRedis("external-redis");

        _ = builder.AddAuthentik("authentik", password)
            .WithReference(externalRedis);

        var resources = builder.Resources.ToList();
        Assert.DoesNotContain(resources, r => r.Name == "authentik-redis");
        Assert.Contains(resources, r => r.Name == "external-redis");
    }

    [Fact]
    public void WithDataVolume_AddsContainerMountAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var password = builder.AddParameter("admin-password", secret: true);

        var authentik = builder.AddAuthentik("authentik", password)
            .WithDataVolume();

        var mounts = authentik.Resource.Annotations.OfType<ContainerMountAnnotation>().ToList();
        Assert.NotEmpty(mounts);
        Assert.Contains(mounts, m => m.Target == "/var/lib/authentik");
    }

    [Fact]
    public void AddAuthentik_ConfiguresSecretKeyParameter_WhenNotProvided()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var password = builder.AddParameter("admin-password", secret: true);

        _ = builder.AddAuthentik("authentik", password);

        var resources = builder.Resources.ToList();
        Assert.Contains(resources, r => r is ParameterResource p && p.Name == "authentik-secret-key");
        Assert.Contains(resources, r => r.Name == "authentik-redis");
    }

}

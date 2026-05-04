using Aspire.Hosting;

namespace Parithon.Aspire.Hosting.Authentik.Tests;

public class AuthentikPublicApiTests
{
    [Fact]
    public void AddAuthentik_Extension_IsPresent()
    {
        var addMethod = typeof(AuthentikResourceBuilderExtensions)
            .GetMethod(
                "AddAuthentik",
                [
                    typeof(IDistributedApplicationBuilder),
                    typeof(string),
                    typeof(global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.ApplicationModel.ParameterResource>),
                    typeof(int?),
                    typeof(global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.ApplicationModel.ParameterResource>)
                ]);

        Assert.NotNull(addMethod);
    }

    [Fact]
    public void WithDataVolume_Extension_IsPresent()
    {
        var method = typeof(AuthentikResourceBuilderExtensions)
            .GetMethod(
                "WithDataVolume",
                [
                    typeof(global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.ApplicationModel.AuthentikResource>),
                    typeof(string)
                ]);

        Assert.NotNull(method);
    }

    [Fact]
    public void WithReference_Redis_Extension_IsPresent()
    {
        var method = typeof(AuthentikResourceBuilderExtensions)
            .GetMethod(
                "WithReference",
                [
                    typeof(global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.ApplicationModel.AuthentikResource>),
                    typeof(global::Aspire.Hosting.ApplicationModel.IResourceBuilder<global::Aspire.Hosting.ApplicationModel.RedisResource>)
                ]);

        Assert.NotNull(method);
    }

}

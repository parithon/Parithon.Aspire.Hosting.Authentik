using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Parithon.Aspire.Hosting.Authentik.Tests;

public class AuthentikResourceBuilderExtensionsTests
{
  [Fact]
  public void AddAuthentik_WithDefaultOptions_AddsResourceToBuilder()
  {
    var builder = DistributedApplication.CreateBuilder();
    var authentik = builder.AddAuthentik("authentik");

    Assert.NotNull(authentik);
    Assert.IsType<AuthentikResource>(authentik.Resource);
    Assert.Equal("authentik", authentik.Resource.Name);
  }

  [Fact]
  public void AddAuthentik_WithNullBuilder_ThrowsArgumentNullException()
  {
    IDistributedApplicationBuilder builder = null!;

    Assert.Throws<ArgumentNullException>(() =>
      builder.AddAuthentik("authentik"));
  }

  [Fact]
  public void AddAuthentik_WithNullName_ThrowsArgumentNullException()
  {
    var builder = DistributedApplication.CreateBuilder();

    Assert.Throws<ArgumentNullException>(() =>
      builder.AddAuthentik(null!));
  }

  [Fact]
  public void AddAuthentik_WithExplicitAdminUsername_SetsAdminUserNameParameter()
  {
    var builder = DistributedApplication.CreateBuilder();
    var usernameParam = builder.AddParameter("admin");
    var authentik = builder.AddAuthentik("authentik", adminUsername: usernameParam);

    Assert.Same(usernameParam.Resource, authentik.Resource.AdminUserNameParameter);
  }

  [Fact]
  public void AddAuthentik_WithExplicitAdminPassword_SetsAdminPasswordParameter()
  {
    var builder = DistributedApplication.CreateBuilder();
    var passwordParam = builder.AddParameter("password", secret: true);
    var authentik = builder.AddAuthentik("authentik", adminPassword: passwordParam);

    Assert.Same(passwordParam.Resource, authentik.Resource.AdminPasswordParameter);
  }

  [Fact]
  public void AddAuthentik_WithNoAdminPassword_CreatesDefaultPasswordParameter()
  {
    var builder = DistributedApplication.CreateBuilder();
    var authentik = builder.AddAuthentik("authentik");

    Assert.NotNull(authentik.Resource.AdminPasswordParameter);
    Assert.Equal("authentik-password", authentik.Resource.AdminPasswordParameter.Name);
  }

  [Fact]
  public void AddAuthentik_HasHttpEndpoint()
  {
    var builder = DistributedApplication.CreateBuilder();
    var authentik = builder.AddAuthentik("authentik");

    var endpoints = authentik.Resource.Annotations.OfType<EndpointAnnotation>().ToList();
    Assert.Contains(endpoints, e => e.UriScheme == "http" && e.TargetPort == 9000);
  }

  [Fact]
  public void AddAuthentik_HasManagementEndpoint()
  {
    var builder = DistributedApplication.CreateBuilder();
    var authentik = builder.AddAuthentik("authentik");

    var endpoints = authentik.Resource.Annotations.OfType<EndpointAnnotation>().ToList();
    Assert.Contains(endpoints, e => e.Name == "management" && e.TargetPort == 9300);
  }

  [Fact]
  public void AddAuthentik_WithExplicitPort_SetsPort()
  {
    var builder = DistributedApplication.CreateBuilder();
    var authentik = builder.AddAuthentik("authentik", port: 8080);

    var endpoint = authentik.Resource.Annotations.OfType<EndpointAnnotation>()
      .FirstOrDefault(e => e.TargetPort == 9000);

    Assert.NotNull(endpoint);
    Assert.Equal(8080, endpoint.Port);
  }
}

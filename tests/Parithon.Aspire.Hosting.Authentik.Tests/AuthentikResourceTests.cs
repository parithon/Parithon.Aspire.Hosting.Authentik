using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Parithon.Aspire.Hosting.Authentik.Tests;

public class AuthentikResourceTests
{
  [Fact]
  public void Constructor_WithAdminPassword_SetsAdminPasswordParameter()
  {
    var passwordParam = new ParameterResource("password", _ => "secret", secret: true);
    var resource = new AuthentikResource("authentik", null, passwordParam);

    Assert.Same(passwordParam, resource.AdminPasswordParameter);
  }

  [Fact]
  public void Constructor_WithAdminUsername_SetsAdminUserNameParameter()
  {
    var usernameParam = new ParameterResource("admin", _ => "myadmin", secret: false);
    var passwordParam = new ParameterResource("password", _ => "secret", secret: true);
    var resource = new AuthentikResource("authentik", usernameParam, passwordParam);

    Assert.Same(usernameParam, resource.AdminUserNameParameter);
  }

  [Fact]
  public void Constructor_WithNullAdminUsername_AdminUserNameParameterIsNull()
  {
    var passwordParam = new ParameterResource("password", _ => "secret", secret: true);
    var resource = new AuthentikResource("authentik", null, passwordParam);

    Assert.Null(resource.AdminUserNameParameter);
  }

  [Fact]
  public void Constructor_WithNullAdminPassword_ThrowsArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() =>
      new AuthentikResource("authentik", null, null!));
  }

  [Fact]
  public void EnabledFeatures_InitiallyEmpty()
  {
    var passwordParam = new ParameterResource("password", _ => "secret", secret: true);
    var resource = new AuthentikResource("authentik", null, passwordParam);

    Assert.Empty(resource.EnabledFeatures);
  }

  [Fact]
  public void DisabledFeatures_InitiallyEmpty()
  {
    var passwordParam = new ParameterResource("password", _ => "secret", secret: true);
    var resource = new AuthentikResource("authentik", null, passwordParam);

    Assert.Empty(resource.DisabledFeatures);
  }
}

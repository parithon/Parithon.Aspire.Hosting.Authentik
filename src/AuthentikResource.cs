namespace Aspire.Hosting.ApplicationModel;

public sealed class AuthentikResource(string name, ParameterResource? admin, ParameterResource adminPassword)
  : ContainerResource(name), IResourceWithServiceDiscovery
{
  private const string DefaultAdmin = "admin";
  internal const string PrimaryEndpointName = "tcp";

  internal HashSet<string> EnabledFeatures { get; } = [];

  internal HashSet<string> DisabledFeatures { get; } = [];

  public ParameterResource? AdminUserNameParameter { get; } = admin;

  internal ReferenceExpression AdminReference =>
    AdminUserNameParameter is not null
      ? ReferenceExpression.Create($"{AdminUserNameParameter}")
      : ReferenceExpression.Create($"{DefaultAdmin}");

  public ParameterResource AdminPasswordParameter { get; } = adminPassword ?? throw new ArgumentNullException(nameof(adminPassword));
}
using Aspire.Hosting.ApplicationModel;
using static Parithon.Aspire.Hosting.Authentik.AuthentikContainerImageTags;

namespace Aspire.Hosting;

public static class AuthentikResourceBuilderExtensions
{
  public static IResourceBuilder<AuthentikResource> AddAuthentik(
    this IDistributedApplicationBuilder builder,
    [ResourceName] string name,
    int? port = null,
    IResourceBuilder<ParameterResource>? adminUsername = null,
    IResourceBuilder<ParameterResource>? adminPassword = null)
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(name);

    var passwordParameter = adminPassword?.Resource
      ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

    var resource = new AuthentikResource(name, adminUsername?.Resource, passwordParameter);

    var authentik = builder
      .AddResource(resource)
      .WithImage(Image)
      .WithImageRegistry(Registry)
      .WithImageTag(Tag)
      .WithHttpEndpoint(port: port, targetPort: DefaultContainerPort)
      .WithHttpEndpoint(targetPort: ManagementInterfaceContainerPort, name: ManagementEndpointName)
      .WithEndpoint(ManagementEndpointName, e => e.ExcludeReferenceEndpoint = true);

    return authentik;
  }
}
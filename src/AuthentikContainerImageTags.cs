namespace Parithon.Aspire.Hosting.Authentik;

internal static class AuthentikContainerImageTags
{
  public const string Registry = "authentik";
  public const string Image = "server";
  public const string Tag = "";
  public const int ContainerUser = 1000;
  public const int ContainerGroup = 1000;
  public const int DefaultContainerPort = 9000;
  public const int ManagementInterfaceContainerPort = 9300;
  public const string ManagementEndpointName = "management";
}
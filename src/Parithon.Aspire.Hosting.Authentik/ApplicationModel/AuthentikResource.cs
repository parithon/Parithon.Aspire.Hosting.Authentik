namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Authentik container resource in the Aspire application model.
/// </summary>
public sealed class AuthentikResource([ResourceName] string name) : ContainerResource(name)
{
    internal const string HttpEndpointName = "http";
    internal const string HttpsEndpointName = "https";

    private EndpointReference? _httpEndpoint;

    /// <summary>
    /// Gets the primary HTTP endpoint exposed by the Authentik resource.
    /// </summary>
    public EndpointReference HttpEndpoint => _httpEndpoint ??= new EndpointReference(this, HttpEndpointName);
}

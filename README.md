# Parithon.Aspire.Hosting.Authentik

[![CI](https://github.com/parithon/Parithon.Aspire.Hosting.Authentik/actions/workflows/ci.yml/badge.svg)](https://github.com/parithon/Parithon.Aspire.Hosting.Authentik/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Parithon.Aspire.Hosting.Authentik.svg)](https://www.nuget.org/packages/Parithon.Aspire.Hosting.Authentik)

A [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) hosting integration for [Authentik](https://goauthentik.io/), an open-source Identity Provider. Adds an `AddAuthentik` extension to wire up an Authentik container resource in your Aspire AppHost.

## Requirements

- .NET 10 SDK
- .NET Aspire 13.x

## Installation

```shell
dotnet add package Parithon.Aspire.Hosting.Authentik
```

## Usage

In your Aspire AppHost project, call `AddAuthentik` on the `IDistributedApplicationBuilder`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var authentik = builder.AddAuthentik("authentik");

builder.AddProject<Projects.MyApp>("myapp")
       .WithReference(authentik);

builder.Build().Run();
```

### Parameters

| Parameter         | Type                                        | Default              | Description                                      |
|-------------------|---------------------------------------------|----------------------|--------------------------------------------------|
| `name`            | `string`                                    | *(required)*         | Resource name used for service discovery.        |
| `port`            | `int?`                                      | `null` (random)      | Host port mapped to the Authentik HTTP endpoint. |
| `adminUsername`   | `IResourceBuilder<ParameterResource>?`      | `"admin"`            | Parameter resource for the admin username.       |
| `adminPassword`   | `IResourceBuilder<ParameterResource>?`      | Auto-generated       | Parameter resource for the admin password.       |

### Custom admin credentials

```csharp
var adminUser = builder.AddParameter("authentik-admin-user", secret: false);
var adminPass = builder.AddParameter("authentik-admin-pass", secret: true);

var authentik = builder.AddAuthentik(
    "authentik",
    adminUsername: adminUser,
    adminPassword: adminPass);
```

## License

This project is licensed under the [MIT License](LICENSE).

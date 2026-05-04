# Parithon.Aspire.Hosting.Authentik

Aspire hosting integration for Authentik with:

- Bootstrap admin configuration
- Default child Postgres provisioning
- External Postgres override via `WithReference(postgresDb)`
- External Redis override via `WithReference(redis)`
- Optional HTTP port override (defaults to `9000`)
- Authentik data persistence helper (`WithDataVolume`)

## Usage

### Default mode

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var adminPassword = builder.AddParameter("authentik-admin-password", secret: true);

var authentik = builder.AddAuthentik("authentik", adminPassword);
```

### External Postgres override

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var adminPassword = builder.AddParameter("authentik-admin-password", secret: true);
var postgres = builder.AddPostgres("postgres");
var postgresDb = postgres.AddDatabase("authdb");

var authentik = builder.AddAuthentik("authentik", adminPassword)
    .WithReference(postgresDb);
```

### External Postgres and Redis override

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var adminPassword = builder.AddParameter("authentik-admin-password", secret: true);
var postgres = builder.AddPostgres("postgres");
var postgresDb = postgres.AddDatabase("authdb");
var redis = builder.AddRedis("redis");

var authentik = builder.AddAuthentik("authentik", adminPassword)
    .WithReference(postgresDb)
    .WithReference(redis);
```

### Secret key handling and data volume

`AddAuthentik(...)` automatically configures `AUTHENTIK_SECRET_KEY`.
At startup, a unique secret is generated when no external parameter value is supplied, and written to the console.

```csharp
var authentik = builder.AddAuthentik("authentik", adminPassword)
    .WithDataVolume();
```

## Sample AppHost

See:

- `samples/Parithon.Aspire.Hosting.Authentik.AppHost/AppHost.cs`
- `samples/Parithon.Aspire.Hosting.Authentik.AppHost/README.md`

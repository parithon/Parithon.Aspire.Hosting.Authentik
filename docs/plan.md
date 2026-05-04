# Plan: Aspire Authentik Hosting Resource

## Goal

Build a new `Parithon.Aspire.Hosting.Authentik` hosting integration package,
modeled on `Aspire.Hosting.Keycloak`.

The v1 API is centered on `AddAuthentik(...)` with bootstrapped admin
configuration, storage mounting, Postgres integration, endpoint metadata,
health metadata, and an in-repo sample AppHost for integration testing and
end-user guidance.

All implementation source code for the Authentik resource must live under
`src/`.

## Authentik-Specific Decisions

- Bootstrap setup uses `AUTHENTIK_BOOTSTRAP_PASSWORD`,
  `AUTHENTIK_BOOTSTRAP_EMAIL`, and optional
  `AUTHENTIK_BOOTSTRAP_TOKEN`.
- Initial setup is headless via environment variables.
- `WithDataVolume()` handles storage persistence only.
- `AddAuthentik` accepts an optional `port` override.
- Default HTTP port is `9000` when `port` is not provided.
- Default listening is HTTP `9000` (or override) and HTTPS `9443`.
- Default database behavior: `AddAuthentik(...)` provisions child Postgres
  server and database resources.
- Override database behavior:
  `AddAuthentik(...).WithReference(postgresDb)` uses an explicit external
  Postgres database resource.
- Override graph rule: omit child Postgres resources when an external
  `PostgresDatabaseResource` reference is present.

## Steps

### Phase 1: Repository Setup

1. Create `src/Parithon.Aspire.Hosting.Authentik/Parithon.Aspire.Hosting.Authentik.csproj`
   targeting `net10.0` with package metadata aligned to Aspire hosting packages.
2. Add baseline Aspire hosting dependencies and pin versions to `13.2.4`.
3. Reserve `samples/` for runnable sample AppHosts and integration-test hosts.
4. Keep all library implementation code in `src/`.
5. Add `README.md` usage guidance, including stable port rationale.

### Phase 2: Resource Model and Core API

1. Create `ApplicationModel/AuthentikResource.cs` as a `ContainerResource`
   derived model with admin settings and endpoint accessors.
2. Create `AuthentikContainerImageTags.cs` with `Registry`, `Image`, `Tag`,
   and UID/GID constants.
3. Create `AuthentikResourceBuilderExtensions.cs` and implement:
   `AddAuthentik(this IDistributedApplicationBuilder builder, string name,
   IResourceBuilder<ParameterResource> adminPassword, int? port = null,
   IResourceBuilder<ParameterResource>? adminUsername = null)`.
4. Keep a single `AddAuthentik` API surface.
5. Do not add `AddAuthentik` overloads that accept `postgresDb`.
6. Set `AUTHENTIK_BOOTSTRAP_PASSWORD` from `adminPassword`.
7. Set `AUTHENTIK_BOOTSTRAP_EMAIL` from `adminUsername` or default to `admin`.
8. Implement effective Postgres source selection:
   - Default mode: create child Postgres server and database resources and map
     `AUTHENTIK_POSTGRESQL__*` from the child database.
   - Override mode: when `.WithReference(postgresDb)` is attached, omit child
     Postgres resources and map `AUTHENTIK_POSTGRESQL__*` from external DB.
9. Emit exactly one effective `AUTHENTIK_POSTGRESQL__*` source.
10. Add explicit endpoint metadata and health-check wiring.
11. Ensure dependency ordering points to the effective Postgres source.
12. Gate Authentik startup on effective database readiness before Authentik
   starts. Default mode waits for the auto-provisioned child Postgres
   database. Override mode waits for the externally referenced `postgresDb`.

### Phase 3: Helper API Scope

1. Add `WithDataVolume(name?)` for Authentik data path mounting,
   for example `/var/lib/authentik`.
2. Use `VolumeNameGenerator` when `name` is not provided.
3. Keep `WithDataVolume` storage-only.
4. Do not implement `WithPostgres` helpers in v1.

### Phase 4: Sample AppHost for Testing and End Users

1. Create `samples/Parithon.Aspire.Hosting.Authentik.AppHost/Parithon.Aspire.Hosting.Authentik.AppHost.csproj`.
2. Create `samples/Parithon.Aspire.Hosting.Authentik.AppHost/AppHost.cs`.
3. Reference local library project from sample AppHost.
4. Keep the sample AppHost suitable for two goals:
   - integration smoke and CI validation
   - copy-ready usage sample for package consumers
5. Demonstrate both wiring patterns:
   - `builder.AddAuthentik(...)`
   - `builder.AddAuthentik(...).WithReference(postgresDb)`
6. Add sample settings and launch profile files for repeatable runs.
7. Add sample run instructions using:
   - `aspire start --isolated`
   - `aspire describe`
   - `aspire wait`

### Phase 5: Hardening, Docs, and Tests

1. Add public API tests for argument validation, annotations, endpoint metadata,
   and manifest snapshots.
2. Add tests for admin parameter handling.
3. Add tests for Postgres behavior in default and override modes.
4. Add tests that verify startup wait semantics against the effective
   database resource in both modes. Default mode waits on the
   auto-provisioned child Postgres database. Override mode waits on
   external `postgresDb`.
5. Add tests for optional port behavior:
   - Default port is `9000`.
   - Explicit `port` overrides endpoint binding.
6. Add integration smoke tests for sample AppHost startup and health.
7. Update `README.md` with examples for default mode, override mode,
   optional port override, and `WithDataVolume` usage.

## Relevant Files

- `/Users/parithon/Repos/Parithon.Aspire.Hosting.Authentik/README.md`
- `/Users/parithon/Repos/Parithon.Aspire.Hosting.Authentik/src/Parithon.Aspire.Hosting.Authentik/Parithon.Aspire.Hosting.Authentik.csproj`
- `/Users/parithon/Repos/Parithon.Aspire.Hosting.Authentik/src/Parithon.Aspire.Hosting.Authentik/ApplicationModel/AuthentikResource.cs`
- `/Users/parithon/Repos/Parithon.Aspire.Hosting.Authentik/src/Parithon.Aspire.Hosting.Authentik/AuthentikResourceBuilderExtensions.cs`
- `/Users/parithon/Repos/Parithon.Aspire.Hosting.Authentik/src/Parithon.Aspire.Hosting.Authentik/AuthentikContainerImageTags.cs`
- `/Users/parithon/Repos/Parithon.Aspire.Hosting.Authentik/samples/Parithon.Aspire.Hosting.Authentik.AppHost/Parithon.Aspire.Hosting.Authentik.AppHost.csproj`
- `/Users/parithon/Repos/Parithon.Aspire.Hosting.Authentik/samples/Parithon.Aspire.Hosting.Authentik.AppHost/AppHost.cs`
- `/Users/parithon/Repos/Parithon.Aspire.Hosting.Authentik/samples/Parithon.Aspire.Hosting.Authentik.AppHost/README.md`
- `/Users/parithon/Repos/Parithon.Aspire.Hosting.Authentik/tests/Parithon.Aspire.Hosting.Authentik.Tests/AuthentikPublicApiTests.cs`
- `/Users/parithon/Repos/Parithon.Aspire.Hosting.Authentik/tests/Parithon.Aspire.Hosting.Authentik.Tests/AuthentikResourceBuilderTests.cs`

## Verification

1. Run `dotnet restore` and `dotnet build` from repo root.
2. Run `dotnet test` for Authentik hosting test projects.
3. Validate manifest output for both modes:
   - `AddAuthentik(name, adminPassword)`
   - `AddAuthentik(name, adminPassword).WithReference(postgresDb)`
4. Run sample AppHost with `aspire start --isolated`.
5. Verify resources with `aspire describe`.
6. Verify readiness with `aspire wait` for Authentik and Postgres resources.
7. Confirm initial setup endpoint is reachable at
   `<http://localhost:9000/if/flow/initial-setup/>` or override port.
8. Confirm no Authentik library implementation code exists outside `src/`.

## Decisions

- Scope: library-first in `Parithon.Aspire.Hosting.Authentik`.
- v1 API: single `AddAuthentik` signature with optional port and username.
- Postgres model: default child Postgres provisioning.
- External DB support: `WithReference(postgresDb)` only.
- Override graph behavior: omit child Postgres when external DB is referenced.
- Health checks: explicitly wired in `AddAuthentik`.
- Persistence: `WithDataVolume` is storage-only.
- Stable port: default HTTP `9000` for OIDC authority consistency.
- Implementation location: all source implementation under `src/`.

## Further Considerations

1. Confirm canonical Authentik health and readiness endpoints.
2. Decide whether to include polyglot exports in v1 or defer to v1.1.
3. Evaluate whether future helper APIs improve discoverability beyond
   `WithReference`.

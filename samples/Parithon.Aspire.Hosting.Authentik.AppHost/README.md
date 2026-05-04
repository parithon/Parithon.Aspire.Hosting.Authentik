# Parithon.Aspire.Hosting.Authentik.AppHost Sample

This AppHost demonstrates two Authentik wiring modes:

- Default mode: child Postgres is provisioned by `AddAuthentik(...)`
- Override mode: external Postgres and Redis are supplied with `.WithReference(...)`

## Run

```bash
aspire start --isolated --apphost samples/Parithon.Aspire.Hosting.Authentik.AppHost/Parithon.Aspire.Hosting.Authentik.AppHost.csproj
```

## Verify resources

```bash
aspire describe
aspire wait authentik-default
aspire wait authentik-external
```

## Notes

- `authentik-default` runs on HTTP port `9000`.
- `authentik-external` runs on HTTP port `9010`.
- Both resources generate `AUTHENTIK_SECRET_KEY` automatically and use `WithDataVolume()` to persist Authentik data.

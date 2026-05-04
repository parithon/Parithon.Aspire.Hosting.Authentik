var builder = DistributedApplication.CreateBuilder(args);

var adminPassword = builder.AddParameter("authentik-admin-password", secret: true);
builder.AddAuthentik("authentik-default", adminPassword);

var externalPostgresDb = builder.AddPostgres("external-postgres")
  .AddDatabase("external-db");
var externalRedis = builder.AddRedis("external-redis");
builder.AddAuthentik("authentik-external", adminPassword, port: 9010)
  .WithReference(externalPostgresDb)
  .WithReference(externalRedis);

builder.Build().Run();

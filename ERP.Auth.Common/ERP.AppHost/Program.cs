var builder = DistributedApplication.CreateBuilder(args);

// ─── 受訓證件作業 ─────────────────────────────────────────────────────────────
var tcs = builder.AddProject<Projects.TCS_Web>("tcs")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// ─── YARP Gateway（唯一對外入口，固定 port 8180）─────────────────────────────
builder.AddProject<Projects.ERP_Gateway>("gateway")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port       = 8180;
        endpoint.IsExternal = true;
    })
    .WithReference(tcs);

builder.Build().Run();

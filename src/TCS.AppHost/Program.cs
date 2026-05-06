var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TCS_Web>("tcs-web")
    .WithReplicas(1); // ExpiryScanService 為定時任務，禁止多副本

builder.Build().Run();

var builder = DistributedApplication.CreateBuilder(args);

// Reference the existing PostgreSQL database (running via docker-compose)
// Instead of creating a new PostgreSQL container, we'll use connection string
var meetingDb = builder.AddConnectionString("meetingmanagement");

// Add the Web application
var webApp = builder.AddProject<Projects.MeetingManagementSystem_Web>("meetingmanagement-web")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(meetingDb);

builder.Build().Run();

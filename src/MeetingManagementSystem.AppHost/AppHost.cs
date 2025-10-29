var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var meetingDb = postgres.AddDatabase("meetingmanagement");

// Add the Web application
var webApp = builder.AddProject<Projects.MeetingManagementSystem_Web>("meetingmanagement-web")
    .WithReference(meetingDb)
    .WaitFor(meetingDb);

builder.Build().Run();

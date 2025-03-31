using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var rabbitPassword = builder.AddParameter("rabbit-password", value: "n1Eb(y*2PUj+md7Jv}PjC~", secret: true);

var rabbit = builder.AddRabbitMQ("bus", password:rabbitPassword)
    .WithManagementPlugin(port:5640);

var postgres = builder.AddPostgres("postgres", port:5432)
    .WithDataVolume(isReadOnly: false);

var postgresdb = postgres.AddDatabase("hyperquant");

var rabbitMigrator = builder.AddProject<Hyperquant_RabbitMigrator>("rabbit-migrator")
    .WithReference(rabbit)
    .WaitFor(rabbit);

var postgresMigrator = builder.AddProject<Hyperquant_Migrator>("postgres-migrator")
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

builder.AddProject<Hyperquant_Repository>("repository")
    
    .WithReference(rabbit)
    .WithReference(postgresdb)
    .WaitFor(rabbit)
    .WaitFor(postgresdb)
    .WaitFor(rabbitMigrator)
    .WaitFor(postgresMigrator);

builder.AddProject<Hyperquant_Gateway>("gateway")
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WaitFor(rabbitMigrator);

builder.AddProject<Hyperquant_Jobs>("jobs")
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WaitFor(rabbitMigrator);

builder.AddProject<Hyperquant_Bybit>("bybit")
    .WithReference(rabbit)
    .WithEnvironment("BYBIT_API_KEY", "xKNXjtfF0RVscwGwSO")
    .WithEnvironment("BYBIT_API_SECRET", "3hQCX0Ao0nMEmlVPZkztquHTv02IpyMzZP1V")
    .WaitFor(rabbit)
    .WaitFor(rabbitMigrator);

builder.Build().Run();
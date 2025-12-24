var builder = DistributedApplication.CreateBuilder(args);

var configApi = builder.AddProject<Projects.ConfigApi>("configapi");

var producerApi = builder.AddProject<Projects.ProducerApi>("producerapi");
producerApi.WithReference(configApi);

var consumerApi = builder.AddProject<Projects.ConsumerApi>("consumerapi");
consumerApi.WithReference(producerApi);

var managementDashboard = builder.AddProject<Projects.ManagementDashboard>("managementdashboard");
managementDashboard.WithReference(configApi);
managementDashboard.WithReference(consumerApi);


builder.Build().Run();

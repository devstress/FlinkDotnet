var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka");
kafka.WithKafkaUI();

builder.Build().Run();


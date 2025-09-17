var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka");
kafka.WithKafkaUI();

await builder.Build().RunAsync();
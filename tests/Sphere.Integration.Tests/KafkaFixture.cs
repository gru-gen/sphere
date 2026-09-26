using Testcontainers.Kafka;

namespace Sphere.Integration.Tests;

public sealed class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.4.1")
        .Build();

    public string BootstrapServers => _container.GetBootstrapAddress();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

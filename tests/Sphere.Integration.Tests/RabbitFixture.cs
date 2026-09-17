using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.RabbitMq;

namespace Sphere.Integration.Tests;

public sealed class RabbitFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithImage("rabbitmq:4.1-management")
        .WithUsername("sphere")
        .WithPassword("sphere-dev")
        .Build();

    public string Host => _container.Hostname;

    public ushort Port => _container.GetMappedPublicPort(5672);

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}


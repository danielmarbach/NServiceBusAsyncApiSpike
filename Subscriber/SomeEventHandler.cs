using NServiceBus;

public class SomeEventHandler : IHandleMessages<SomeEvent>
{
    public Task Handle(SomeEvent message, IMessageHandlerContext context)
    {
        Console.WriteLine("Received Some Event");
        return Task.CompletedTask;
    }
}
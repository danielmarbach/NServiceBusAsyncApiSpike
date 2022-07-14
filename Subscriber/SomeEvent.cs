using Infrastructure;

[SubscribedEvent(EventName = "SomeEvent", Version = 1)]
public class SomeEvent
{
    public string SomeOtherValue { get; init; }
}
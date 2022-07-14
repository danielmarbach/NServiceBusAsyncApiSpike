using Infrastructure;

[PublishedEvent(EventName = "SomeEvent", Version = 1)]
class SomeEventThatIsBeingPublished
{
    public string SomeValue { get; init; }
    public string SomeOtherValue { get; init; }
}
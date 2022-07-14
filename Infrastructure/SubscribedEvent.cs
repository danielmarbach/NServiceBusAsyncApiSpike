namespace Infrastructure;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SubscribedEvent : Attribute
{
    public string Publisher { get; init; }
    public string EventName { get; init; }
    public int Version { get; init; }
}
using System.Reflection;
using NServiceBus;

namespace Infrastructure;

static class ConventionsExtensions
{
    // This is still a bit ugly and will get improved
    //https://github.com/Particular/NServiceBus/pull/6448
    public static void Add(this Conventions conventions, IMessageConvention convention)
    {
        var methodInfo = typeof(Conventions).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
        methodInfo!.Invoke(conventions, new [] { convention });
    }
}
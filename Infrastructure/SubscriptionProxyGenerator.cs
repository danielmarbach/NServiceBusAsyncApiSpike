using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using NServiceBus;

namespace Infrastructure;

class SubscriptionProxyGenerator
{
    public SubscriptionProxyGenerator()
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("AsyncApiFeatureProxies"), AssemblyBuilderAccess.Run);
        moduleBuilder = assemblyBuilder.DefineDynamicModule("AsyncApiFeatureProxies");
    }

    /// <summary>
    /// Generates the concrete implementation of the given type.
    /// Only properties on the given type are generated in the concrete implementation.
    /// </summary>
    public Type CreateTypeFrom(string typeName)
    {
        var typeBuilder = moduleBuilder.DefineType(typeName + SUFFIX,
            TypeAttributes.Serializable | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
            typeof(object)
        );

        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

        typeBuilder.AddInterfaceImplementation(typeof(IEvent));

        return typeBuilder.CreateTypeInfo().AsType();
    }

    /// <summary>
    /// Given a custom attribute and property builder, adds an instance of the custom attribute
    /// to the property builder
    /// </summary>
    static void AddCustomAttributeToProperty(CustomAttributeData attributeData, PropertyBuilder propBuilder)
    {
        var namedArguments = attributeData.NamedArguments;

        object[] constructorArgs = attributeData.ConstructorArguments.Select(ExtractValue).ToArray();
        if (namedArguments == null)
        {
            var attributeBuilder = new CustomAttributeBuilder(
                attributeData.Constructor,
                constructorArgs);

            propBuilder.SetCustomAttribute(attributeBuilder);
        }
        else
        {
            PropertyInfo[] namedProperties = namedArguments.Select(x => (PropertyInfo)x.MemberInfo).ToArray();
            object[] propertyValues = namedArguments.Select(x => x.TypedValue.Value).ToArray();

            var attributeBuilder = new CustomAttributeBuilder(
                attributeData.Constructor,
                constructorArgs,
                namedProperties,
                propertyValues);

            propBuilder.SetCustomAttribute(attributeBuilder);
        }
    }

    static object ExtractValue(CustomAttributeTypedArgument arg)
    {
        if (arg.Value is ReadOnlyCollection<CustomAttributeTypedArgument> nestedValue)
        {
            return nestedValue.Select(x => x.Value).ToArray();
        }
        return arg.Value;
    }

    ModuleBuilder moduleBuilder;
    internal const string SUFFIX = "";
}
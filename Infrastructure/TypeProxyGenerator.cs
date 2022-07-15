using System.Reflection;
using System.Reflection.Emit;
using NServiceBus;

namespace Infrastructure;

class TypeProxyGenerator
{
    public TypeProxyGenerator()
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("AsyncApiFeatureProxies"), AssemblyBuilderAccess.Run);
        moduleBuilder = assemblyBuilder.DefineDynamicModule("AsyncApiFeatureProxies");
    }

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

    ModuleBuilder moduleBuilder;
    internal const string SUFFIX = "";
}
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;

namespace Viotto.Tests.Mock;


internal static class AssemblyHelper
{
    private static readonly AssemblyName assemblyName;
    private static readonly AssemblyBuilder assemblyBuilder;
    private static readonly ModuleBuilder moduleBuilder;

    static AssemblyHelper()
    {
        assemblyName = new AssemblyName($"{nameof(Mock)}.Types");
        assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.FullName);
    }

    // TODO (Bruno Viotto):
    // ! Allow to use mocked type constructors!
    public static T CreateMockedType<T>()
    {
        return (T)CreateMockedType(typeof(T));
    }

    public static object CreateMockedType(Type type)
    {
        var typeBuilder = DefineMockedType(type);
        var constructorBuilder = DefineMockedTypeConstructor(typeBuilder);

        DefineMockedMethods(type, typeBuilder, constructorBuilder);
        DefineMockedProperties(type, typeBuilder, constructorBuilder);

        var ctorIl = constructorBuilder.GetILGenerator();
        ctorIl.Emit(OpCodes.Ret);

        var mockedType = typeBuilder.CreateType();
        return Activator.CreateInstance(mockedType)!;
    }

    private static TypeBuilder DefineMockedType(Type type)
    {
        return moduleBuilder.DefineType(
            $"{type.Name}_Mock",
            TypeAttributes.Public | TypeAttributes.Class,
            null,
            new[] { type }
        );
    }

    private static ConstructorBuilder DefineMockedTypeConstructor(TypeBuilder type)
    {
        return type.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.HasThis,
            null
        );
    }

    private static void DefineMockedMethods(Type type, TypeBuilder mockedType, ConstructorBuilder constructor)
    {
        var ctorIl = constructor.GetILGenerator();

        foreach (var method in type.GetMethods())
        {
            var methodBuilder = mockedType.DefineMethod(
                method.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                method.CallingConvention,
                method.ReturnType,
                method.GetParameters().Select(x => x.ParameterType).ToArray()
            );

            var il = methodBuilder.GetILGenerator();

            if (method.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Ret);
                return;
            }

            var returnTableField = DefineReturnTableField(mockedType, method);

            // base.constructor();
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Array.Empty<Type>())!);

            // {methodName}_Field = new Dictionary<{methodArgumentTypes},{methodReturnType}>();
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Newobj, returnTableField.FieldType.GetConstructor(Array.Empty<Type>())!);
            ctorIl.Emit(OpCodes.Stfld, returnTableField);

            /* Loads {methodName}_Field into the stack */
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, returnTableField);

            /* Loads all method arguments in the stack */
            for (int i = 1; i <= method.GetParameters().Length; i++)
                il.Emit(OpCodes.Ldarg_S, i);

            // new Tuple<{methodArgumentTypes}>({methodArguments});
            il.Emit(OpCodes.Newobj, TupleHelper.GetConstructor(method.GetParameters().Select(x => x.ParameterType)));
            // {methodName}_Field[{tuple}];
            il.Emit(OpCodes.Callvirt, returnTableField.FieldType.GetMethod("get_Item")!);

            // return;
            il.Emit(OpCodes.Ret);
        }
    }

    private static FieldBuilder DefineReturnTableField(TypeBuilder mockedType, MethodInfo methodToMock)
    {
        var returnTableKeyType = methodToMock.GetParameters()
            .Select(x => x.ParameterType)
            .ToTupleType();

        var returnTableFieldName = $"{methodToMock.Name}_ReturnTable";
        var returnTableType = typeof(Dictionary<,>).MakeGenericType(returnTableKeyType, methodToMock.ReturnType);

        return mockedType.DefineField(
            returnTableFieldName,
            returnTableType,
            FieldAttributes.Private
        );
    }

    private static void DefineMockedProperties(Type type, TypeBuilder mockedType, ConstructorBuilder constructor)
    {
        foreach (var property in type.GetProperties())
        {
            mockedType.DefineProperty(
                property.Name,
                property.Attributes,
                property.PropertyType,
                property.GetIndexParameters().Select(x => x.ParameterType).ToArray()
            );
        }
    }
}

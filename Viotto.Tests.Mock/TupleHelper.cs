using System.Reflection;

namespace Viotto.Tests.Mock;


internal static class TupleHelper
{
    private static readonly Type[] tupleTypes = Enumerable.Range(1, 8)
        .Select(x => Type.GetType($"{typeof(Tuple).FullName}`{x}")!)
        .Prepend(typeof(Tuple))
        .ToArray();

    public static ConstructorInfo GetConstructor(this IEnumerable<Type> self)
    {
        var types = self.ToArray();

        var constructor = tupleTypes[self.Count()]
            .MakeGenericType(types)
            .GetConstructor(types)!;

        return constructor;
    }

    public static Type ToTupleType(this IEnumerable<Type> self)
    {
        var tupleType = tupleTypes[self.Count()]
            .MakeGenericType(self.ToArray());

        return tupleType;
    }

    public static object ToTuple(this IEnumerable<object> self)
    {
        var tupleGenericTypes = self.Select(x => x.GetType()).ToArray();

        var tupleCreateMethod = typeof(Tuple).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.Name == nameof(Tuple.Create))
            .Where(x => x.IsGenericMethod)
            .Single(x => x.GetGenericArguments().Length == tupleGenericTypes.Length)
            .MakeGenericMethod(tupleGenericTypes);

        return tupleCreateMethod.Invoke(tupleGenericTypes[tupleGenericTypes.Length], self.ToArray())!;
    }
}

namespace vsc.CodeAnalysisUtills.Extensions
{
    /// <summary>
    /// Provides extension methods for inspecting <see cref="Type"/> instances and type hierarchies.
    /// </summary>
    public static class SystemTypeExtensions
    {
        /// <summary>
        /// Determines whether the type is a <see cref="Nullable{T}"/> value type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns><see langword="true"/> if the type is <see cref="Nullable{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsNullableValueType(this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// Determines whether the type is a primitive, enum, <see cref="string"/>, or <see cref="decimal"/>, including their nullable forms.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns><see langword="true"/> if the type is a basic type; otherwise, <see langword="false"/>.</returns>
        public static bool IsBasicType(this Type? type)
        {
            if (type == null) return false;

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType.IsPrimitive || underlyingType.IsEnum || underlyingType == typeof(string) || underlyingType == typeof(decimal);
        }

        /// <summary>
        /// Determines whether the type is a generic type that is or implements <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns><see langword="true"/> if the type is or implements <see cref="IEnumerable{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsIEnumerable(this Type? type)
        {
            return IsOrImplementsGenericInterface(type, typeof(IEnumerable<>));
        }

        /// <summary>
        /// Determines whether the type is a generic type that is or implements <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns><see langword="true"/> if the type is or implements <see cref="ICollection{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsICollection(this Type? type)
        {
            return IsOrImplementsGenericInterface(type, typeof(ICollection<>));
        }

        /// <summary>
        /// Determines whether the type is <see cref="List{T}"/>.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns><see langword="true"/> if the type is <see cref="List{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsList(this Type? type)
        {
            return type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static bool IsOrImplementsGenericInterface(Type? type, Type interfaceDefinition)
        {
            if (type == null) return false;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == interfaceDefinition) return true;

            return type.GetInterfaces().Any(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == interfaceDefinition);
        }

        /// <summary>
        /// Filters the types to those assignable to <paramref name="baseType"/>.
        /// </summary>
        /// <param name="types">The types to filter.</param>
        /// <param name="baseType">The base type to compare against.</param>
        /// <param name="includeBaseType"><see langword="true"/> to include <paramref name="baseType"/> itself in the results.</param>
        /// <returns>The distinct types assignable to <paramref name="baseType"/>.</returns>
        public static IEnumerable<Type> WhereInheritsFrom(this IEnumerable<Type> types, Type baseType, bool includeBaseType = false)
        {
            ArgumentNullException.ThrowIfNull(types);
            ArgumentNullException.ThrowIfNull(baseType);
            return types.Where(t => t.InheritsFrom(baseType, includeBaseType)).Distinct();
        }

        /// <summary>
        /// Determines whether the type inherits from <paramref name="baseType"/>.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="baseType">The base type to compare against.</param>
        /// <param name="includeBaseType"><see langword="true"/> to also match when the type is <paramref name="baseType"/>.</param>
        /// <returns><see langword="true"/> if the type inherits from <paramref name="baseType"/>; otherwise, <see langword="false"/>.</returns>
        public static bool InheritsFrom(this Type type, Type baseType, bool includeBaseType = false)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(baseType);
            return (includeBaseType || type != baseType) && baseType.IsAssignableFrom(type);
        }

        /// <summary>
        /// Filters the types to those for which <paramref name="parentType"/> is a base type.
        /// </summary>
        /// <param name="types">The types to filter.</param>
        /// <param name="parentType">The type to check as a base type.</param>
        /// <param name="includeBaseType"><see langword="true"/> to include <paramref name="parentType"/> itself in the results.</param>
        /// <returns>The distinct types for which <paramref name="parentType"/> is a base type.</returns>
        public static IEnumerable<Type> WhereIsBaseTypeOf(this IEnumerable<Type> types, Type parentType, bool includeBaseType = false)
        {
            ArgumentNullException.ThrowIfNull(types);
            ArgumentNullException.ThrowIfNull(parentType);
            return types.Where(t => parentType.IsBaseTypeOf(t, includeBaseType)).Distinct();
        }

        /// <summary>
        /// Determines whether the type is a base type of <paramref name="childType"/>.
        /// </summary>
        /// <param name="type">The type to check as a base type.</param>
        /// <param name="childType">The type to compare against.</param>
        /// <param name="includeChildType"><see langword="true"/> to also match when the type is <paramref name="childType"/>.</param>
        /// <returns><see langword="true"/> if the type is a base type of <paramref name="childType"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsBaseTypeOf(this Type type, Type childType, bool includeChildType = false)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(childType);
            return (includeChildType || type != childType) && type.IsAssignableFrom(childType);
        }

        /// <summary>
        /// Returns the base types of the type, from the nearest to the furthest ancestor.
        /// </summary>
        /// <param name="type">The type whose base types are returned.</param>
        /// <param name="universe">If specified, only base types contained in this collection are returned.</param>
        /// <returns>The base types of <paramref name="type"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static IEnumerable<Type> BaseTypes(this Type type, IEnumerable<Type>? universe = null)
        {
            ArgumentNullException.ThrowIfNull(type);

            var allowedTypes = universe?.ToHashSet();
            var currentType = type.BaseType;
            while (currentType != null)
            {
                if (allowedTypes == null || allowedTypes.Contains(currentType))
                {
                    yield return currentType;
                }
                currentType = currentType.BaseType;
            }
        }

        /// <summary>
        /// Returns the types in <paramref name="universe"/> that derive from the type.
        /// </summary>
        /// <param name="type">The base type to search for descendants of.</param>
        /// <param name="universe">The types to search. If <see langword="null"/>, an empty sequence is returned.</param>
        /// <returns>The types in <paramref name="universe"/> that derive from <paramref name="type"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static IEnumerable<Type> DescendantTypes(this Type type, IEnumerable<Type>? universe = null)
        {
            ArgumentNullException.ThrowIfNull(type);

            var descendants = universe?.Where(t => t.IsSubclassOf(type)) ?? Enumerable.Empty<Type>();
            return descendants;
        }

        /// <summary>
        /// Returns the types whose base type is not in the collection.
        /// </summary>
        /// <param name="types">The types to filter.</param>
        /// <returns>The distinct types whose base type is not in <paramref name="types"/>.</returns>
        public static IEnumerable<Type> RootElements(this IEnumerable<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);
            var typeSet = types.ToHashSet();
            return typeSet.Where(t => t.BaseType == null || !typeSet.Contains(t.BaseType)).ToList();
        }

        /// <summary>
        /// Groups the types by their root type within the collection.
        /// </summary>
        /// <param name="types">The types to group.</param>
        /// <returns>
        /// A dictionary keyed by root type
        /// </returns>
        public static Dictionary<Type, IEnumerable<Type>> GroupByRootElements(this IEnumerable<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);
            var typeSet = types.ToHashSet();
            var roots = typeSet.RootElements().OrderBy(root => root.Name, StringComparer.Ordinal);
            var groups = new Dictionary<Type, IEnumerable<Type>>();

            foreach (var root in roots)
            {
                var descendants = typeSet.WhereInheritsFrom(root).OrderBy(type => type.Name, StringComparer.Ordinal).ToList();
                groups.Add(root, descendants);
            }

            return groups;
        }
    }
}

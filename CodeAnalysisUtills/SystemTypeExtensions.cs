
namespace CodeAnalysisUtills.Extensions
{
    /// <summary>
    /// Provides extension methods for inspecting <see cref="Type"/> instances and type hierarchies.
    /// </summary>
    public static class SystemTypeExtensions
    {
        /// <summary>
        /// Determines whether the type is a <see cref="Nullable{T}"/> value type.
        /// </summary>
        public static bool IsNullable(this Type type)
        {
            // Check for Nullable<T> (value types like int? are nullable)
            if (Nullable.GetUnderlyingType(type) != null) return true;

            return false; // Assume non-nullable if it's a value type and not Nullable<T>
        }
        /// <summary>
        /// Determines whether the type is a primitive, enum, <see cref="string"/>, or <see cref="decimal"/>, including their nullable forms.
        /// </summary>
        public static bool IsBasicType(this Type? type)
        {
            if (type == null || type.IsGenericType) return false;

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType.IsPrimitive || underlyingType.IsEnum || underlyingType == typeof(string) || underlyingType == typeof(float) || underlyingType == typeof(decimal);
        }

        /// <summary>
        /// Determines whether the type is a generic type that is or implements <see cref="IEnumerable{T}"/>.
        /// </summary>
        public static bool IsIEnumerable(this Type? type)
        {
            if (type == null) return false;

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType.IsGenericType && (underlyingType.GetInterface(typeof(IEnumerable<>).Name) != null || underlyingType.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        /// <summary>
        /// Determines whether the type is a generic type that is or implements <see cref="ICollection{T}"/>.
        /// </summary>
        public static bool IsICollection(this Type? type)
        {
            if (type == null) return false;

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return !type.IsBasicType() && underlyingType.IsGenericType && (underlyingType.GetInterface(typeof(ICollection<>).Name) != null || underlyingType.GetGenericTypeDefinition() == typeof(ICollection<>));
        }

        /// <summary>
        /// Determines whether the type is <see cref="List{T}"/>.
        /// </summary>
        public static bool IsList(this Type? type)
        {
            if (type == null) return false;

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return !type.IsBasicType() && underlyingType.IsGenericType && (underlyingType.GetInterface(typeof(List<>).Name) != null || underlyingType.GetGenericTypeDefinition() == typeof(List<>));
        }

        /// <summary>
        /// Filters the types to those assignable to <paramref name="baseType"/>.
        /// </summary>
        /// <param name="includeBaseType"><see langword="true"/> to include <paramref name="baseType"/> itself in the results.</param>
        public static IEnumerable<Type> WhereInheritsFrom(this IEnumerable<Type> types, Type baseType, bool includeBaseType = false)
            => types.Where(t => includeBaseType ? baseType.IsAssignableFrom(t) : t != baseType && baseType.IsAssignableFrom(t)).Distinct();

        /// <summary>
        /// Determines whether the type inherits from <paramref name="baseType"/>.
        /// </summary>
        /// <param name="includeBaseType"><see langword="true"/> to also match when the type is <paramref name="baseType"/>.</param>
        public static bool InheritsFrom(this Type type, Type baseType, bool includeBaseType = false)
            => (includeBaseType ? type == baseType : type != baseType) && baseType.IsAssignableFrom(type);

        /// <summary>
        /// Filters the types to those for which <paramref name="parentType"/> is a base type.
        /// </summary>
        /// <param name="includeBaseType"><see langword="true"/> to include <paramref name="parentType"/> itself in the results.</param>
        public static IEnumerable<Type> WhereIsBaseTypeOf(this IEnumerable<Type> types, Type parentType, bool includeBaseType = false)
            => types.Where(t => includeBaseType ? parentType.IsBaseTypeOf(t) : t != parentType && parentType.IsBaseTypeOf(t)).Distinct();

        /// <summary>
        /// Determines whether the type is a base type of <paramref name="childType"/>.
        /// </summary>
        /// <param name="includeChildType"><see langword="true"/> to also match when the type is <paramref name="childType"/>.</param>
        public static bool IsBaseTypeOf(this Type type, Type childType, bool includeChildType = false)
           => (includeChildType ? type == childType : type != childType) && type.IsAssignableFrom(childType);

        /// <summary>
        /// Returns the base types of the type, from the nearest to the furthest ancestor.
        /// </summary>
        /// <param name="universe">If specified, only base types contained in this collection are returned.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static IEnumerable<Type> BaseTypes(this Type type, IEnumerable<Type>? universe = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var currentType = type.BaseType;
            while (currentType != null)
            {
                if (universe == null || universe.Contains(currentType))
                {
                    yield return currentType;
                }
                currentType = currentType.BaseType;
            }
        }

        /// <summary>
        /// Returns the types in <paramref name="universe"/> that derive from the type.
        /// </summary>
        /// <param name="universe">The types to search. If <see langword="null"/>, an empty sequence is returned.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static IEnumerable<Type> DescendantTypes(this Type type, IEnumerable<Type>? universe = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var descendants = universe?.Where(t => t.IsSubclassOf(type)) ?? Enumerable.Empty<Type>();
            return descendants;
        }
        /// <summary>
        /// Returns the types whose base type is not in the collection.
        /// </summary>
        public static IEnumerable<Type> RootElements(this IEnumerable<Type> types)
            => types.Where(t => t.BaseType == null || !types.Contains(t.BaseType)).Distinct();

        /// <summary>
        /// Groups the types by their root type within the collection.
        /// </summary>
        /// <returns>
        /// A dictionary keyed by root type, where each value contains the types that inherit from that root, ordered by name.
        /// </returns>
        public static Dictionary<Type, IEnumerable<Type>> GroupByRootElements(this IEnumerable<Type> types)
            => types.RootElements().OrderBy(r => r.Name).ToDictionary(root => root, root => (IEnumerable<Type>)types.WhereInheritsFrom(root).OrderBy(t => t.Name));

        }
    }
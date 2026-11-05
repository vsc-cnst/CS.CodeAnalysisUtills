using System.Reflection;

namespace vsc.CodeAnalysisUtills.Extensions
{
    /// <summary>
    /// Provides extension methods for inspecting <see cref="MemberInfo"/> instances.
    /// </summary>
    public static class MemberInfoExtensions
    {
        /// <summary>
        /// Gets the type of a property or field, or the return type of a method.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>
        /// The member's data type, or <see langword="null"/> if the member is not a property, field, or method.
        /// </returns>
        public static Type? GetMemberDataType(this MemberInfo? member) => member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            MethodInfo method => method.ReturnType,
            _ => null
        };

        /// <summary>
        /// Determines whether the member is a field or property of a basic type.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member is a field or property of a basic type; otherwise, <see langword="false"/>.</returns>
        /// <seealso cref="SystemTypeExtensions.IsBasicType(Type?)"/>
        public static bool MemberIsBasic(this MemberInfo? member)
        {
            return member switch
            {
                FieldInfo field => field.FieldType.IsBasicType(),
                PropertyInfo property => property.PropertyType.IsBasicType(),
                _ => false
            };
        }

        /// <summary>
        /// Determines whether the member is a field or property of a class type that is not an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member is a field or property of a class type that is not an <see cref="IEnumerable{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool MemberIsClass(this MemberInfo? member)
        {
            var type = member switch
            {
                FieldInfo field => field.FieldType,
                PropertyInfo property => property.PropertyType,
                _ => null
            };

            return type is { IsClass: true } && !type.IsIEnumerable();
        }

        /// <summary>
        /// Determines whether the member is a field or property of type <see cref="List{T}"/>.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member is a field or property of type <see cref="List{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool MemberIsList(this MemberInfo? member)
        {
            return member switch
            {
                FieldInfo field => field.FieldType.IsList(),
                PropertyInfo property => property.PropertyType.IsList(),
                _ => false
            };
        }

        /// <summary>
        /// Determines whether the member is a field or property whose type implements <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member is a field or property whose type implements <see cref="ICollection{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool MemberIsICollection(this MemberInfo? member)
        {
            return member switch
            {
                FieldInfo field => field.FieldType.IsICollection(),
                PropertyInfo property => property.PropertyType.IsICollection(),
                _ => false
            };
        }

        /// <summary>
        /// Determines whether the member is a field or property whose type implements <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member is a field or property whose type implements <see cref="IEnumerable{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool MemberIsIEnumerable(this MemberInfo? member)
        {
            return member switch
            {
                FieldInfo field => field.FieldType.IsIEnumerable(),
                PropertyInfo property => property.PropertyType.IsIEnumerable(),
                _ => false
            };
        }

        /// <summary>
        /// Determines whether the member is a method.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member is a method; otherwise, <see langword="false"/>.</returns>
        public static bool MemberIsMethod(this MemberInfo? member) => member is MethodInfo;
    }

    /// <summary>
    /// Compares <see cref="MemberInfo"/> instances by name and declaring type.
    /// </summary>
    public class MemberInfoNameTypeComparer : IEqualityComparer<MemberInfo>
    {
        /// <inheritdoc/>
        public bool Equals(MemberInfo? x, MemberInfo? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x == null || y == null)
                return false;

            return x.Name == y.Name && x.DeclaringType == y.DeclaringType;
        }

        /// <inheritdoc/>
        public int GetHashCode(MemberInfo obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return HashCode.Combine(obj.Name, obj.DeclaringType);
        }
    }
}

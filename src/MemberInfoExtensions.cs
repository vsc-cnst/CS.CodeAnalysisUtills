using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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
        public static Type? GetMemberDataType(this MemberInfo member) => member switch
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
        public static bool MemberIsBasic(this MemberInfo member)
        {
            return member != null && (
                    (member is FieldInfo f && f.FieldType.IsBasicType())
                    || (member is PropertyInfo p && p.PropertyType.IsBasicType())
                );
        }

        /// <summary>
        /// Determines whether the member is a field or property of a class type that is not an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member is a field or property of a class type that is not an <see cref="IEnumerable{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool MemberIsClass(this MemberInfo member)
        {
            return member != null
                && (
                        (member is FieldInfo f && f.FieldType.IsClass)
                        || (member is PropertyInfo p && p.PropertyType.IsClass)
                    )
                && !member.MemberIsIEnumerable();
        }

        /// <summary>
        /// Determines whether the member is a field or property of type <see cref="List{T}"/>.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member is a field or property of type <see cref="List{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool MemberIsList(this MemberInfo member)
        {
            return member != null && (
                (member is FieldInfo f && f.FieldType.IsList())
                || (member is PropertyInfo p && p.PropertyType.IsList())
            );
        }

        /// <summary>
        /// Determines whether the member is a field or property whose type implements <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member is a field or property whose type implements <see cref="ICollection{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool MemberIsICollection(this MemberInfo member)
        {
            return member != null && (
                    (member is FieldInfo f && f.FieldType.IsICollection())
                    || (member is PropertyInfo p && p.PropertyType.IsICollection())
                );
        }

        /// <summary>
        /// Determines whether the member is a field or property whose type implements <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member is a field or property whose type implements <see cref="IEnumerable{T}"/>; otherwise, <see langword="false"/>.</returns>
        public static bool MemberIsIEnumerable(this MemberInfo member)
        {
            return member != null && (
                    (member is FieldInfo f && f.FieldType.IsIEnumerable())
                    || (member is PropertyInfo p && p.PropertyType.IsIEnumerable())
                );
        }

        /// <summary>
        /// Determines whether the member is a method.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member is a method; otherwise, <see langword="false"/>.</returns>
        public static bool MemberIsMethod(this MemberInfo member) => member != null && (member is MethodInfo);

    }

    /// <summary>
    /// Compares <see cref="MemberInfo"/> instances by name and declaring type.
    /// </summary>
    public class MemberInfoNameTypeComparer : IEqualityComparer<MemberInfo>
    {
        /// <inheritdoc/>
        public bool Equals(MemberInfo? x, MemberInfo? y)
        {
            if (x == null || y == null)
                return false;

            return x.Name == y.Name && x.DeclaringType == y.DeclaringType;
        }

        /// <inheritdoc/>
        public int GetHashCode(MemberInfo obj)
        {
            using (MD5 md5 = MD5.Create())
                unchecked
                {
                    byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(obj.Name + obj.DeclaringType?.FullName ?? ""));
                    int positiveHash = Math.Abs(BitConverter.ToInt32(bytes, 0));
                    return (positiveHash == int.MinValue) ? 0 : positiveHash;
                }
        }
    }
}
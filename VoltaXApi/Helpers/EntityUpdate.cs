using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using VoltaXApi.Exceptions;
using VoltaXApi.Models;

namespace VoltaXApi.Helpers;

/// <summary>
/// Copies the fields of a JSON request body onto a tracked entity, limited to plain scalar
/// properties that are neither on the central deny list nor marked [NotUpdatable].
/// Fields the client may not set are ignored; navigation properties are never followed.
/// </summary>
public static class EntityUpdate
{
    private static readonly HashSet<string> DeniedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ID", "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt", "CreatedBy", "CreatedByID",
        // ownership
        "UserID", "PartnerID", "OwnerID",
        // authorisation
        "RoleID", "Role", "IsAdmin", "IsSuperAdmin", "IsEmailVerified", "IsPhoneNumberVerified",
    };

    private static readonly Regex DeniedPattern = new(
        "balance$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CredentialPattern = new(
        "password|hash$|salt$|secret|verificationtoken|verificationcode|refreshtoken|providertoken",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Fields no client payload may ever set, on create or update.</summary>
    public static bool IsCredential(string propertyName) => CredentialPattern.IsMatch(propertyName);

    public static bool IsUpdatable(PropertyInfo property) =>
        property.CanRead && property.CanWrite && property.SetMethod!.IsPublic &&
        property.GetIndexParameters().Length == 0 &&
        IsScalar(property.PropertyType) &&
        !DeniedNames.Contains(property.Name) &&
        !DeniedPattern.IsMatch(property.Name) && !IsCredential(property.Name) &&
        property.GetCustomAttribute<NotUpdatableAttribute>() == null &&
        property.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() == null;

    /// <returns>The names of the properties that were copied.</returns>
    public static List<string> Apply<T>(T target, JsonElement body, JsonSerializerOptions options) where T : class
    {
        if (body.ValueKind != JsonValueKind.Object) throw new ValidationException("Expected a JSON object.");

        var updatable = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(IsUpdatable)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var copied = new List<string>();
        foreach (var field in body.EnumerateObject())
        {
            if (!updatable.TryGetValue(field.Name, out var property)) continue;
            try
            {
                property.SetValue(target, field.Value.Deserialize(property.PropertyType, options));
                copied.Add(property.Name);
            }
            catch (Exception e) when (e is JsonException or NotSupportedException or ArgumentException)
            {
                throw new ValidationException($"Invalid value for '{field.Name}'.");
            }
        }
        return copied;
    }

    private static bool IsScalar(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) ||
               t == typeof(DateTimeOffset) || t == typeof(DateOnly) || t == typeof(TimeOnly) || t == typeof(Guid) ||
               t == typeof(TimeSpan);
    }
}

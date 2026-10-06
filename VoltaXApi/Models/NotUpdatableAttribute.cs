namespace VoltaXApi.Models
{
    /// <summary>
    /// Marks a property the generic PUT endpoint must never copy from the request body
    /// (it is only changed by a dedicated, authorised code path). Complements the central
    /// deny list in <see cref="VoltaXApi.Helpers.EntityUpdate"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NotUpdatableAttribute : Attribute
    {
    }
}

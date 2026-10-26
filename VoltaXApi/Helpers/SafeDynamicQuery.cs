using System.Collections;
using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text.RegularExpressions;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Helpers;

/// <summary>
/// Applies the sort / search / filter parts of <see cref="GlobalParams"/> to a query.
///
/// Client input never reaches the Dynamic LINQ parser as code: every field name is resolved
/// against the entity's real public properties (the canonical name from reflection is what
/// goes into the expression) and every value is bound as a typed @n parameter.
/// Anything that does not resolve is rejected with a <see cref="ValidationException"/> (400).
/// </summary>
public static class SafeDynamicQuery
{
    private const int MaxPathLength = 128;
    private const int MaxTerms = 20;

    private static readonly Regex Identifier = new("^[A-Za-z_][A-Za-z0-9_]{0,63}$", RegexOptions.Compiled);

    // Never sortable, searchable or filterable: sorting or filtering on these turns the list
    // endpoint into an oracle for the value.
    private static readonly Regex SensitiveName = new(
        "password|hash|salt|secret|cvv|verificationtoken|resetpassword|providertoken|refreshtoken|apikey",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly ParsingConfig Config = new()
    {
        AllowNewToEvaluateAnyType = false,
        EvaluateGroupByAtDatabase = true
    };

    public static IQueryable<T> Apply<T>(IQueryable<T> data, GlobalParams p)
    {
        data = ApplyFilter(data, p);
        data = ApplySearch(data, p);
        return ApplySort(data, p);
    }

    public static IQueryable<T> ApplySort<T>(IQueryable<T> data, GlobalParams p)
    {
        if (string.IsNullOrWhiteSpace(p.OrderBy)) return data;

        var descending = (p.ReverseOrder ?? "n").Trim().ToLowerInvariant() switch
        {
            "" or "n" or "asc" => false,
            "y" or "desc" => true,
            _ => throw new ValidationException("Sort direction must be asc or desc.")
        };

        var path = Resolve(typeof(T), p.OrderBy, '.');
        if (path.ThroughCollection) throw new ValidationException($"Cannot sort by '{p.OrderBy}'.");

        return data.OrderBy(Config, path.Expression + (descending ? " descending" : ""));
    }

    public static IQueryable<T> ApplySearch<T>(IQueryable<T> data, GlobalParams p)
    {
        if (p.SearchBy == null || p.SearchBy.Length == 0 || string.IsNullOrEmpty(p.SearchValue)) return data;
        if (p.SearchBy.Length > MaxTerms) throw new ValidationException("Too many search fields.");

        var clauses = new List<string>();
        foreach (var field in p.SearchBy)
        {
            var path = Resolve(typeof(T), field, '.');
            if (path.ThroughCollection) throw new ValidationException($"Cannot search by '{field}'.");
            if (path.LeafType != typeof(string)) continue;
            clauses.Add($"({path.NullGuard}{path.Expression} != null && {path.Expression}.Contains(@0))");
        }

        return clauses.Count == 0 ? data : data.Where(Config, string.Join(" || ", clauses), p.SearchValue);
    }

    public static IQueryable<T> ApplyFilter<T>(IQueryable<T> data, GlobalParams p)
    {
        if (p.FilterBy == null || p.FilterBy.Length == 0) return data;
        if (p.FilterBy.Length > MaxTerms) throw new ValidationException("Too many filter fields.");
        if (p.FilterValue == null || p.FilterValue.Length < p.FilterBy.Length)
            throw new ValidationException("Each filter field needs a value.");

        var joiner = (p.FilterMethod ?? "||").Trim().ToLowerInvariant() switch
        {
            "||" or "or" => " || ",
            "&&" or "and" => " && ",
            _ => throw new ValidationException("Filter method must be || or &&.")
        };

        if (p.FilterOperator != null && p.FilterOperator.Length > p.FilterBy.Length)
            throw new ValidationException("More filter operators than filter fields.");

        var direct = new List<string>();
        var nested = new List<string>();
        var values = new List<object?>();

        for (int i = 0; i < p.FilterBy.Length; i++)
        {
            var field = p.FilterBy[i];
            // "Nav.Prop" filters on a collection navigation, "Nav-Prop" on a reference navigation.
            var path = field.Contains('-') ? Resolve(typeof(T), field, '-') : Resolve(typeof(T), field, '.');
            if (field.Contains('-') && path.ThroughCollection) throw new ValidationException($"Cannot filter by '{field}'.");

            var op = ParseOperator(p.FilterOperator != null && i < p.FilterOperator.Length ? p.FilterOperator[i] : null, path.LeafType, field);
            var placeholder = "@" + values.Count;
            if (op == FilterOp.Null)
                // Presence check: no value to bind, the expression itself says null or not null.
                placeholder = ParseFlag(p.FilterValue[i], field) ? "null" : "!null";
            else
                values.Add(op == FilterOp.In
                    ? ConvertList(p.FilterValue[i], path.LeafType, field)
                    : ConvertValue(p.FilterValue[i], path.LeafType, field));

            if (path.ThroughCollection)
                nested.Add($"{path.CollectionExpression}.Any({Comparison(path.InnerExpression, op, placeholder)})");
            else if (path.IsNested)
                nested.Add($"({path.NullGuard}{Comparison(path.Expression, op, placeholder)})");
            else
                direct.Add(Comparison(path.Expression, op, placeholder));
        }

        var parts = new List<string>();
        if (direct.Count > 0) parts.Add("(" + string.Join(joiner, direct) + ")");
        parts.AddRange(nested);

        return data.Where(Config, string.Join(" && ", parts), values.ToArray());
    }

    private enum FilterOp { Eq, Neq, Contains, StartsWith, Gt, Gte, Lt, Lte, In, Null }

    private const int MaxInValues = 50;

    private static FilterOp ParseOperator(string? raw, Type leafType, string field)
    {
        var op = (raw ?? "").Trim().ToLowerInvariant() switch
        {
            "" or "eq" => FilterOp.Eq,
            "neq" => FilterOp.Neq,
            "contains" => FilterOp.Contains,
            "startswith" => FilterOp.StartsWith,
            "gt" => FilterOp.Gt,
            "gte" => FilterOp.Gte,
            "lt" => FilterOp.Lt,
            "lte" => FilterOp.Lte,
            "in" => FilterOp.In,
            "null" => FilterOp.Null,
            _ => throw new ValidationException($"Unknown filter operator for '{field}'.")
        };

        var t = Nullable.GetUnderlyingType(leafType) ?? leafType;
        if (op == FilterOp.Null && leafType.IsValueType && Nullable.GetUnderlyingType(leafType) == null)
            throw new ValidationException($"'{field}' always has a value.");
        var isText = t == typeof(string);
        if ((op is FilterOp.Contains or FilterOp.StartsWith) && !isText)
            throw new ValidationException($"'{field}' can only be compared, not searched.");
        // Ordering comparisons on text would follow the database collation: only numbers, dates and enums.
        if ((op is FilterOp.Gt or FilterOp.Gte or FilterOp.Lt or FilterOp.Lte) && (isText || t == typeof(bool) || t == typeof(Guid)))
            throw new ValidationException($"'{field}' cannot be compared by range.");
        return op;
    }

    private static string Comparison(string expression, FilterOp op, string placeholder) => op switch
    {
        FilterOp.Eq => $"{expression} == {placeholder}",
        FilterOp.Neq => $"{expression} != {placeholder}",
        FilterOp.Contains => $"({expression} != null && {expression}.Contains({placeholder}))",
        FilterOp.StartsWith => $"({expression} != null && {expression}.StartsWith({placeholder}))",
        FilterOp.Gt => $"{expression} > {placeholder}",
        FilterOp.Gte => $"{expression} >= {placeholder}",
        FilterOp.Lt => $"{expression} < {placeholder}",
        FilterOp.Lte => $"{expression} <= {placeholder}",
        FilterOp.In => $"{placeholder}.Contains({expression})",
        FilterOp.Null => placeholder == "null" ? $"{expression} == null" : $"{expression} != null",
        _ => throw new InvalidOperationException()
    };

    private static bool ParseFlag(string? raw, string field) => (raw ?? "").Trim().ToLowerInvariant() switch
    {
        "true" or "1" or "yes" => true,
        "false" or "0" or "no" => false,
        _ => throw new ValidationException($"Invalid value for '{field}'.")
    };

    /// <summary>The '|'-separated values of an "in" filter, as a typed list (so EF translates it to SQL IN).</summary>
    private static object ConvertList(string? raw, Type target, string field)
    {
        var parts = (raw ?? "").Split('|');
        if (parts.Length > MaxInValues) throw new ValidationException($"Too many values for '{field}'.");
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(target))!;
        foreach (var part in parts) list.Add(ConvertValue(part, target, field));
        return list;
    }

    /// <summary>Resolves a client-supplied field path to canonical property names, or throws.</summary>
    public static ResolvedPath Resolve(Type root, string? path, char separator)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > MaxPathLength)
            throw new ValidationException("Invalid field name.");

        var segments = path.Split(separator);
        if (segments.Length > 2) throw new ValidationException($"Unknown field '{path}'.");

        var names = new List<string>();
        var current = root;
        string? collection = null;

        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (!Identifier.IsMatch(segment) || SensitiveName.IsMatch(segment))
                throw new ValidationException($"Unknown field '{path}'.");

            var property = FindProperty(current, segment) ?? throw new ValidationException($"Unknown field '{path}'.");
            names.Add(property.Name);

            var type = property.PropertyType;
            if (i < segments.Length - 1)
            {
                var element = CollectionElementType(type);
                if (element != null)
                {
                    collection = property.Name;
                    current = element;
                }
                else if (type.IsClass && type != typeof(string))
                {
                    current = type;
                }
                else
                {
                    throw new ValidationException($"Unknown field '{path}'.");
                }
            }
            else
            {
                if (!IsScalar(type)) throw new ValidationException($"Unknown field '{path}'.");
                current = type;
            }
        }

        return new ResolvedPath(names, collection, current);
    }

    private static PropertyInfo? FindProperty(Type type, string name)
    {
        var candidates = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && p.CanRead &&
                        string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return candidates.FirstOrDefault(p => p.Name == name) ?? (candidates.Count == 1 ? candidates[0] : null);
    }

    private static Type? CollectionElementType(Type type)
    {
        if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type)) return null;
        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }

    private static bool IsScalar(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) ||
               t == typeof(DateTimeOffset) || t == typeof(DateOnly) || t == typeof(TimeOnly) || t == typeof(Guid) ||
               t == typeof(TimeSpan);
    }

    private static object? ConvertValue(string? raw, Type target, string field)
    {
        var underlying = Nullable.GetUnderlyingType(target);
        if (raw == null || (underlying != null && (raw.Length == 0 || raw.Equals("null", StringComparison.OrdinalIgnoreCase))))
            return null;

        var t = underlying ?? target;
        try
        {
            if (t == typeof(string)) return raw;
            if (t.IsEnum)
            {
                if (Enum.TryParse(t, raw, true, out var parsed) && (Enum.IsDefined(t, parsed!) || t.IsDefined(typeof(FlagsAttribute), false)))
                    return parsed;
                throw new FormatException();
            }
            if (t == typeof(Guid)) return Guid.Parse(raw);
            if (t == typeof(DateTimeOffset)) return DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            if (t == typeof(DateOnly)) return DateOnly.Parse(raw, CultureInfo.InvariantCulture);
            if (t == typeof(TimeOnly)) return TimeOnly.Parse(raw, CultureInfo.InvariantCulture);
            if (t == typeof(TimeSpan)) return TimeSpan.Parse(raw, CultureInfo.InvariantCulture);
            if (t == typeof(DateTime)) return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            return Convert.ChangeType(raw, t, CultureInfo.InvariantCulture);
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            throw new ValidationException($"Invalid value for '{field}'.");
        }
    }

    public sealed class ResolvedPath
    {
        public ResolvedPath(IReadOnlyList<string> names, string? collectionName, Type leafType)
        {
            Names = names;
            CollectionName = collectionName;
            LeafType = leafType;
        }

        public IReadOnlyList<string> Names { get; }
        public string? CollectionName { get; }
        public Type LeafType { get; }
        public bool ThroughCollection => CollectionName != null;
        public bool IsNested => Names.Count > 1;
        // Always rooted at "it." so a property named like a keyword (Parent, Root, ...) stays a member access.
        public string Expression => "it." + string.Join(".", Names);
        public string CollectionExpression => "it." + CollectionName;
        public string InnerExpression => "it." + Names[^1];
        public string NullGuard => IsNested ? $"it.{Names[0]} != null && " : string.Empty;
    }
}

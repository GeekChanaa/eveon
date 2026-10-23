using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace VoltaXApi.Helpers
{
    /// <summary>
    /// Page size limits shared by every paged list. A page never holds more than <see cref="MaxPageSize"/> rows
    /// (configuration "Pagination:MaxPageSize", default 100); a missing, zero or negative page size asks for the
    /// largest page, and a page number below 1 is the first page.
    /// </summary>
    public static class Paging
    {
        public const string MaxPageSizeKey = "Pagination:MaxPageSize";
        public const int DefaultMaxPageSize = 100;

        private static int _maxPageSize = DefaultMaxPageSize;

        /// <summary>Set once at start up from <see cref="MaxPageSizeKey"/>.</summary>
        public static int MaxPageSize
        {
            get => _maxPageSize;
            set => _maxPageSize = value >= 1
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), value, $"{MaxPageSizeKey} must be at least 1.");
        }

        public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize) =>
            (Math.Max(1, pageNumber), pageSize <= 0 ? MaxPageSize : Math.Min(pageSize, MaxPageSize));
    }

    public class PagedList<T> : List<T>
    {
        private static readonly MethodInfo AsNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethod(nameof(EntityFrameworkQueryableExtensions.AsNoTracking))!;

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        private PagedList(IEnumerable<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        /// <summary>
        /// Counts the rows and reads one page of them. Entity Framework queries run asynchronously and without change
        /// tracking (the rows are only read); other sources are paged in memory.
        /// </summary>
        public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default)
        {
            (pageNumber, pageSize) = Paging.Normalize(pageNumber, pageSize);
            if (source.Provider is not IAsyncQueryProvider)
                return Create(source, pageNumber, pageSize);

            source = WithoutTracking(source);
            var count = await source.CountAsync(cancellationToken);
            var skip = (pageNumber - 1) * pageSize;
            var items = count > skip
                ? await source.Skip(skip).Take(pageSize).ToListAsync(cancellationToken)
                : new List<T>();
            return new PagedList<T>(items, count, pageNumber, pageSize);
        }

        /// <summary>Pages rows that are already in memory.</summary>
        public static PagedList<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = Paging.Normalize(pageNumber, pageSize);
            var rows = source as IReadOnlyCollection<T> ?? source.ToList();
            var items = rows.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return new PagedList<T>(items, rows.Count, pageNumber, pageSize);
        }

        private static IQueryable<T> WithoutTracking(IQueryable<T> source) =>
            typeof(T).IsValueType
                ? source
                : source.Provider.CreateQuery<T>(Expression.Call(null, AsNoTrackingMethod.MakeGenericMethod(typeof(T)), source.Expression));
    }
}

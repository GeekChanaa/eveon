namespace VoltaXApi.Helpers
{
    /// <summary>
    /// Paging, sorting, search and filter query parameters of the list endpoints. The page size is capped by
    /// <see cref="Paging.MaxPageSize"/> when the list is read.
    /// </summary>
    public class GlobalParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public string? OrderBy { get; set; }
        public string[]? SearchBy { get; set; }
        public string? SearchValue { get; set; }
        public string[]? FilterBy { get; set; }
        public string[]? FilterValue { get; set; }
        /// <summary>
        /// Comparison per filter, parallel to <see cref="FilterBy"/>: eq (default when missing), neq, contains,
        /// startswith, gt, gte, lt, lte, in (the value lists the accepted values separated by '|'), or null
        /// (value true: the field has no value, false: it has one).
        /// </summary>
        public string[]? FilterOperator { get; set; }
        public string FilterMethod { get; set; } = "||";

        public string? ReverseOrder { get; set; }
    }
}

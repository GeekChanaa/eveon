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
        public string FilterMethod { get; set; } = "||";

        public string? ReverseOrder { get; set; }
    }
}

namespace VoltaXApi.Helpers
{
    /// <summary>Values of the semicolon separated exports.</summary>
    public static class CsvField
    {
        /// <summary>Quotes a value that contains the separator, quotes or a line break; neutralises spreadsheet formulas.</summary>
        public static string Of(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            if (value[0] is '=' or '+' or '-' or '@')
                value = "'" + value;
            return value.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
    }
}

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace VoltaXApi.Helpers
{
    /// <summary>
    /// A semicolon separated file (UTF-8 with BOM, as the other exports) written row by row to the response while
    /// the rows are read, so a large export never sits in memory. Every value goes through <see cref="CsvField.Of"/>.
    /// </summary>
    public sealed class CsvStreamResult<T> : IActionResult
    {
        private readonly string _fileName;
        private readonly IReadOnlyList<string> _header;
        private readonly IAsyncEnumerable<T> _rows;
        private readonly Func<T, IEnumerable<string?>> _fields;

        public CsvStreamResult(string fileName, IReadOnlyList<string> header, IAsyncEnumerable<T> rows, Func<T, IEnumerable<string?>> fields)
        {
            _fileName = fileName;
            _header = header;
            _rows = rows;
            _fields = fields;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            var cancellationToken = context.HttpContext.RequestAborted;
            response.ContentType = "text/csv; charset=utf-8";
            response.Headers[HeaderNames.ContentDisposition] = new ContentDispositionHeaderValue("attachment") { FileName = _fileName, FileNameStar = _fileName }.ToString();

            await using var writer = new StreamWriter(response.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), 16 * 1024, leaveOpen: true);
            await writer.WriteLineAsync(string.Join(";", _header.Select(CsvField.Of)));
            var count = 0;
            await foreach (var row in _rows.WithCancellation(cancellationToken))
            {
                await writer.WriteLineAsync(string.Join(";", _fields(row).Select(Field)));
                // Hand the bytes to the client regularly instead of buffering the whole file.
                if (++count % 500 == 0) await writer.FlushAsync(cancellationToken);
            }
            await writer.FlushAsync(cancellationToken);
        }

        /// <summary>Numbers are written as they are (a negative amount is not a formula); text is quoted and neutralised.</summary>
        private static string Field(string? value) =>
            value != null && System.Text.RegularExpressions.Regex.IsMatch(value, @"^-?\d+(\.\d+)?$") ? value : CsvField.Of(value);
    }
}

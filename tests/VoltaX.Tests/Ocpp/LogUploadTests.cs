using System.Security.Cryptography;
using System.Text;
using VoltaXApi.Models.Ocpp201;
using VoltaXApi.OCPP.Services;

namespace VoltaX.Tests.Ocpp;

/// <summary>
/// Charger log uploads (GetLog / GetDiagnostics): the one-time ticket in the upload URL and the storage of the file.
/// </summary>
public class LogUploadTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
    private readonly string _root = Path.Combine(Path.GetTempPath(), "voltax-logs-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Tickets_are_random_URL_safe_256_bit_tokens_stored_only_as_a_hash()
    {
        var token = LogUploadTickets.NewToken();

        Assert.Equal(43, token.Length);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('=', token);
        Assert.NotEqual(token, LogUploadTickets.NewToken());
        var hash = LogUploadTickets.Hash(token);
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, LogUploadTickets.Hash(token));
        Assert.NotEqual(hash, LogUploadTickets.Hash(token + "x"));
        Assert.DoesNotContain(token, hash);
    }

    [Fact]
    public void A_ticket_is_valid_once_before_it_expires()
    {
        var ticket = new LogUploadTicket { TokenHash = LogUploadTickets.Hash(LogUploadTickets.NewToken()), ChargePointID = "CP-1", RequestId = 4, CreatedAt = Now, ExpiresAt = Now.AddHours(2) };

        Assert.Equal(LogUploadRejection.None, LogUploadTickets.Validate(ticket, Now.AddMinutes(5)));
        Assert.Equal(LogUploadRejection.Expired, LogUploadTickets.Validate(ticket, Now.AddHours(2)));
        Assert.Equal(LogUploadRejection.UnknownTicket, LogUploadTickets.Validate(null, Now));
        ticket.UsedAt = Now.AddMinutes(1);
        Assert.Equal(LogUploadRejection.AlreadyUsed, LogUploadTickets.Validate(ticket, Now.AddMinutes(2)));
    }

    [Fact]
    public void Charger_ids_extensions_and_file_names_cannot_escape_the_storage_folder()
    {
        Assert.Equal("_.._etc", LogUploadTickets.SafeSegment("../../etc"));
        Assert.DoesNotContain('/', LogUploadTickets.SafeSegment("a/b\\c"));
        Assert.Equal("_", LogUploadTickets.SafeSegment(".."));
        Assert.Equal("tar", LogUploadTickets.SafeExtension("diag.TAR"));
        Assert.Equal("log", LogUploadTickets.SafeExtension("x.ex e"));
        Assert.Equal("log", LogUploadTickets.SafeExtension(null));
        Assert.Equal("log", LogUploadTickets.SafeExtension("a.verylongextension"));
        Assert.Equal("secret.zip", LogUploadTickets.SafeFileName("../../secret.zip", "f"));
        Assert.Equal("fallback.log", LogUploadTickets.SafeFileName("", "fallback.log"));
    }

    [Fact]
    public async Task A_stored_upload_records_its_size_and_SHA_256_under_the_charger_folder()
    {
        var storage = new ChargerLogStorage(_root);
        var payload = Encoding.UTF8.GetBytes("line 1\nline 2\n");

        var stored = await storage.SaveAsync(new MemoryStream(payload), "CP/1", "log", 1024, CancellationToken.None);

        Assert.Equal(payload.Length, stored.SizeBytes);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(), stored.Sha256);
        Assert.Equal(payload, File.ReadAllBytes(storage.FullPath(stored.RelativePath)));
        Assert.StartsWith(Path.GetFullPath(_root), storage.FullPath(stored.RelativePath));
        Assert.StartsWith("CP_1", stored.RelativePath);
    }

    [Fact]
    public async Task An_oversized_upload_is_refused_and_its_partial_file_deleted()
    {
        var storage = new ChargerLogStorage(_root);

        await Assert.ThrowsAsync<LogTooLargeException>(() => storage.SaveAsync(new MemoryStream(new byte[2048]), "CP-1", "log", 1024, CancellationToken.None));

        var folder = Path.Combine(_root, "CP-1");
        Assert.True(!Directory.Exists(folder) || Directory.GetFiles(folder).Length == 0);
    }

    [Fact]
    public void Paths_outside_the_storage_root_are_refused()
    {
        Assert.Throws<InvalidOperationException>(() => new ChargerLogStorage(_root).FullPath("../outside.log"));
    }
}

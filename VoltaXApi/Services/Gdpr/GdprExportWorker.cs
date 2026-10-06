using VoltaXApi.ScaleOut;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Factories;
using VoltaXApi.Models;

namespace VoltaXApi.Services.Gdpr
{
    /// <summary>
    /// Builds the archive for approved data requests, emails the owner a single-use link
    /// and expires archives after <c>Gdpr:ExportLinkDays</c> (7 by default).
    /// </summary>
    public class GdprExportWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan StaleProcessing = TimeSpan.FromMinutes(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GdprExportWorker> _logger;
        private readonly IClusterJobLease _lease;

        public GdprExportWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<GdprExportWorker> logger, IClusterJobLease lease)
        {
            _lease = lease;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Only one replica runs this job (see ScaleOut/ClusterJobLease.cs).
                    if (await _lease.TryAcquireAsync("gdpr-export", TimeSpan.FromMinutes(5), stoppingToken))
                    {
                        await ProcessApprovedAsync(stoppingToken);
                        await ExpireAsync(stoppingToken);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "GDPR export sweep failed");
                }

                try { await Task.Delay(Interval, stoppingToken); }
                catch (TaskCanceledException) { return; }
            }
        }

        private async Task ProcessApprovedAsync(CancellationToken ct)
        {
            List<int> ids;
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                var staleBefore = DateTime.UtcNow - StaleProcessing;
                ids = await db.UserInfoDownloadRequests
                    .Where(r => r.Status == DownloadRequestStatusEnum.Approved ||
                                r.Status == DownloadRequestStatusEnum.Processing && r.UpdatedAt < staleBefore)
                    .OrderBy(r => r.ID).Select(r => r.ID).Take(10).ToListAsync(ct);
            }

            foreach (var id in ids)
                await ProcessOneAsync(id, ct);
        }

        private async Task ProcessOneAsync(int requestId, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
            var exporter = scope.ServiceProvider.GetRequiredService<IUserDataExportService>();
            var request = await db.UserInfoDownloadRequests.Include(r => r.User).SingleAsync(r => r.ID == requestId, ct);
            if (request.User == null || request.User.DeletionRequestedAt != null)
            {
                request.Status = DownloadRequestStatusEnum.Denied;
                await db.SaveChangesAsync(ct);
                return;
            }

            request.Status = DownloadRequestStatusEnum.Processing;
            await db.SaveChangesAsync(ct);

            try
            {
                var fileName = await exporter.BuildArchiveAsync(request.UserID, ct);
                var token = GdprTokens.NewToken();
                var now = DateTime.UtcNow;
                exporter.DeleteArchive(request.ExportFileName);
                request.ExportFileName = fileName;
                request.DownloadTokenHash = GdprTokens.Hash(token);
                request.CompletedAt = now;
                request.ExpiresAt = now.AddDays(Math.Max(1, _configuration.GetValue("Gdpr:ExportLinkDays", 7)));
                request.DownloadedAt = null;
                request.Status = DownloadRequestStatusEnum.Completed;
                await db.SaveChangesAsync(ct);

                var spa = (_configuration["SpaLink"] ?? string.Empty).TrimEnd('/');
                var link = $"{spa}/my-dashboard/profile?tab=privacy&export={Uri.EscapeDataString(token)}";
                var mailFactory = scope.ServiceProvider.GetRequiredService<IMailRequestFactory>();
                var mail = scope.ServiceProvider.GetRequiredService<IMailService>();
                try
                {
                    await mail.SendDownloadInfoRequestApproved(
                        mailFactory.CreateApprovedDownloadInfoRequest(request.User.Email), request.User.FullName, link, request.ExpiresAt.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Data export {RequestID} is ready but the email could not be sent", request.ID);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Data export {RequestID} failed", request.ID);
                request.Status = DownloadRequestStatusEnum.Failed;
                await db.SaveChangesAsync(CancellationToken.None);
            }
        }

        private async Task ExpireAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
            var exporter = scope.ServiceProvider.GetRequiredService<IUserDataExportService>();
            var now = DateTime.UtcNow;
            var expired = await db.UserInfoDownloadRequests
                .Where(r => r.Status == DownloadRequestStatusEnum.Completed && r.ExpiresAt < now)
                .Take(100).ToListAsync(ct);
            if (expired.Count == 0) return;

            foreach (var request in expired)
            {
                exporter.DeleteArchive(request.ExportFileName);
                request.ExportFileName = null;
                request.DownloadTokenHash = null;
                request.Status = DownloadRequestStatusEnum.Expired;
            }
            await db.SaveChangesAsync(ct);
        }
    }
}

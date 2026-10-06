using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;

namespace VoltaXApi.OCPP.Helpers
{
    /// <summary>
    /// Registration state of a charge point, read from the database so it survives restarts and reconnections
    /// without a new BootNotification. A charge point is Accepted once it has a provisioning record
    /// (Automatic, Manual, Skipped or Legacy); without one it is Pending.
    /// </summary>
    public static class ChargePointRegistration
    {
        public static Task<bool> IsAcceptedAsync(VoltaXApiDbContext db, string chargePointId) =>
            db.ChargePointProvisionings.AsNoTracking()
                .AnyAsync(p => p.ChargePoint!.ChargePointId == chargePointId);
    }
}

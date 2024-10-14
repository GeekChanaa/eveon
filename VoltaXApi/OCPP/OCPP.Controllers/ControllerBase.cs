using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using Microsoft.EntityFrameworkCore;

namespace OCPP.Core.Server
{
    public partial class ControllerBase
    {
        protected IConfiguration Configuration { get; set; }

        protected ChargePointStatus ChargePointStatus { get; set; }

        protected ILogger Logger { get; set; }

        public ControllerBase(IConfiguration config, ILoggerFactory loggerFactory, ChargePointStatus chargePointStatus)
        {
            Configuration = config;

            if (chargePointStatus != null)
            {
                ChargePointStatus = chargePointStatus;
            }
            else
            {
                Console.WriteLine("New ControllerBase => empty chargepoint status");
            }
        }


        protected bool UpdateConnectorStatus(int connectorId, string? status, DateTimeOffset? statusTime)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<VoltaXApiDbContext>();
                optionsBuilder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                using (VoltaXApiDbContext dbContext = new VoltaXApiDbContext(optionsBuilder.Options))
                {
                    ConnectorStatus? connectorStatus = dbContext.ConnectorStatuses.Where(u=> u.ChargePointID == ChargePointStatus.Id && connectorId == u.ConnectorID).FirstOrDefault();
                    if (connectorStatus == null)
                    {
                        // no matching entry => create connector status
                        connectorStatus = new ConnectorStatus();
                        connectorStatus.ChargePointID = ChargePointStatus.Id;
                        connectorStatus.ConnectorID = connectorId;
                        Console.WriteLine("UpdateConnectorStatus => Creating new DB-ConnectorStatus: ID={0} / Connector={1}", connectorStatus.ChargePointID, connectorStatus.ConnectorID);
                        dbContext.ConnectorStatuses.Add(connectorStatus);
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        connectorStatus.LastStatus = status;
                        connectorStatus.LastStatusTime = ((statusTime.HasValue) ? statusTime.Value : DateTimeOffset.UtcNow).DateTime;
                    }
                    dbContext.SaveChanges();
                    Console.WriteLine("UpdateConnectorStatus => Save ConnectorStatus: ID={0} / Connector={1} / Status={2}", connectorStatus.ChargePointID, connectorId, status);
                    return true;
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine( "UpdateConnectorStatus => Exception writing connector status (ID={0} / Connector={1}): {2}", ChargePointStatus?.Id, connectorId, exp.Message);
                Console.WriteLine("INNER EXCEPTION : ");
                Console.WriteLine(exp.StackTrace);
                if(exp.InnerException != null)
                    Console.WriteLine(exp.InnerException.ToString());
            }

            return false;
        }

        protected static string CleanChargeTagId(string rawChargeTagId, ILogger logger)
        {
            string idTag = rawChargeTagId;

            // KEBA adds the serial to the idTag ("<idTag>_<serial>") => cut off suffix
            if (!string.IsNullOrWhiteSpace(rawChargeTagId))
            {
                int sep = rawChargeTagId.IndexOf('_');
                if (sep >= 0)
                {
                    idTag = rawChargeTagId.Substring(0, sep);
                    Console.WriteLine("CleanChargeTagId => Charge tag '{0}' => '{1}'", rawChargeTagId, idTag);
                }
            }

            return idTag;
        }

        protected static DateTimeOffset MaxExpiryDate
        {
            get
            {
                return new DateTime(2199, 12, 31);
            }
        }
    }
}

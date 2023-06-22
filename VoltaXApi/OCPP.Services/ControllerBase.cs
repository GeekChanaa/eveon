/*
 * OCPP.Core - https://github.com/dallmann-consulting/OCPP.Core
 * Copyright (C) 2020-2021 dallmann consulting GmbH.
 * All Rights Reserved.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Messages_OCPP20;
using Microsoft.EntityFrameworkCore;

namespace OCPP.Core.Server
{
    public partial class ControllerBase
    {
        /// <summary>
        /// Configuration context for reading app settings
        /// </summary>
        protected IConfiguration Configuration { get; set; }

        /// <summary>
        /// Chargepoint status
        /// </summary>
        protected ChargePointStatus ChargePointStatus { get; set; }

        /// <summary>
        /// ILogger object
        /// </summary>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
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


        /// <summary>
        /// Helper function for creating and updating the ConnectorStatus in then database
        /// </summary>
        protected bool UpdateConnectorStatus(int connectorId, string? status, DateTimeOffset? statusTime)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<VoltaXApiDbContext>();
                optionsBuilder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                using (VoltaXApiDbContext dbContext = new VoltaXApiDbContext(optionsBuilder.Options))
                {
                    ConnectorStatus? connectorStatus = dbContext.ConnectorStatuses.Where(u=> u.ChargePointId == ChargePointStatus.Id && connectorId == u.ConnectorId).FirstOrDefault();
                    if (connectorStatus == null)
                    {
                        // no matching entry => create connector status
                        connectorStatus = new ConnectorStatus();
                        connectorStatus.ChargePointId = ChargePointStatus.Id;
                        connectorStatus.ConnectorId = connectorId;
                        Console.WriteLine("UpdateConnectorStatus => Creating new DB-ConnectorStatus: ID={0} / Connector={1}", connectorStatus.ChargePointId, connectorStatus.ConnectorId);
                        dbContext.Add<ConnectorStatus>(connectorStatus);
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        connectorStatus.LastStatus = status;
                        connectorStatus.LastStatusTime = ((statusTime.HasValue) ? statusTime.Value : DateTimeOffset.UtcNow).DateTime;
                    }
                    dbContext.SaveChanges();
                    Console.WriteLine("UpdateConnectorStatus => Save ConnectorStatus: ID={0} / Connector={1} / Status={2}", connectorStatus.ChargePointId, connectorId, status);
                    return true;
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine( "UpdateConnectorStatus => Exception writing connector status (ID={0} / Connector={1}): {2}", ChargePointStatus?.Id, connectorId, exp.Message);
            }

            return false;
        }

        /// <summary>
        /// Clean charge tag Id from possible suffix ("..._abc")
        /// </summary>
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

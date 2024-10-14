export const OCPPActions = [
  {
    name: "Authorize",
    description: "Authorize a user to start a charging session."
  },
  {
    name: "BootNotification",
    description: "Sent when a charge point boots or reboots to notify the CSMS."
  },
  {
    name: "CancelReservation",
    description: "Cancel a previously made reservation for a charge point."
  },
  {
    name: "CertificateSigned",
    description: "Notify the charge point that a new certificate has been signed."
  },
  {
    name: "ChangeAvailability",
    description: "Change the availability status of a charge point (inoperative/operative)."
  },
  {
    name: "ClearCache",
    description: "Clear the local cache of the charge point."
  },
  {
    name: "ClearChargingProfile",
    description: "Clear a previously set charging profile for a charge point."
  },
  {
    name: "ClearDisplayMessage",
    description: "Clear a message currently displayed on the charge point's screen."
  },
  {
    name: "ClearedChargingLimit",
    description: "Notify that a previously set charging limit has been cleared."
  },
  {
    name: "ClearVariableMonitoring",
    description: "Clear monitoring settings for specific variables at a charge point."
  },
  {
    name: "CostUpdated",
    description: "Notify the charge point of updated energy costs."
  },
  {
    name: "CustomerInformation",
    description: "Send customer-specific information to the CSMS."
  },
  {
    name: "DataTransfer",
    description: "Send proprietary data between the charge point and the CSMS."
  },
  {
    name: "DeleteCertificate",
    description: "Delete an installed security certificate from the charge point."
  },
  {
    name: "FirmwareStatusNotification",
    description: "Notify the CSMS of the status of a firmware update."
  },
  {
    name: "Get15118EVCertificate",
    description: "Retrieve an ISO 15118 compliant EV certificate from the CSMS."
  },
  {
    name: "GetBaseReport",
    description: "Request a basic report from the charge point."
  },
  {
    name: "GetCertificateStatus",
    description: "Request the status of an installed certificate."
  },
  {
    name: "GetChargingProfiles",
    description: "Retrieve active charging profiles from the charge point."
  },
  {
    name: "GetCompositeSchedule",
    description: "Request a charging schedule for a specified time period."
  },
  {
    name: "GetDisplayMessages",
    description: "Retrieve messages currently displayed or scheduled to be displayed."
  },
  {
    name: "GetInstalledCertificateIds",
    description: "Retrieve the list of installed certificates at the charge point."
  },
  {
    name: "GetLocalListVersion",
    description: "Retrieve the version of the local authorization list."
  },
  {
    name: "GetLog",
    description: "Request logs from the charge point."
  },
  {
    name: "GetMonitoringReport",
    description: "Request a report of all monitored variables."
  },
  {
    name: "GetReport",
    description: "Request detailed reports from the charge point."
  },
  {
    name: "GetTransactionStatus",
    description: "Request the current status of a transaction."
  },
  {
    name: "GetVariables",
    description: "Retrieve the current values of variables from the charge point."
  },
  {
    name: "Heartbeat",
    description: "Heartbeat message to check communication between charge point and CSMS."
  },
  {
    name: "InstallCertificate",
    description: "Install a new certificate on the charge point."
  },
  {
    name: "LogStatusNotification",
    description: "Notify the CSMS of the current status of log uploads."
  },
  {
    name: "MeterValues",
    description: "Send meter readings (energy usage data) from the charge point to the CSMS."
  },
  {
    name: "NotifyChargingLimit",
    description: "Notify the CSMS of the current charging limit."
  },
  {
    name: "NotifyCustomerInformation",
    description: "Notify the CSMS with customer-specific information."
  },
  {
    name: "NotifyDisplayMessages",
    description: "Notify the CSMS about display messages at the charge point."
  },
  {
    name: "NotifyEVChargingNeeds",
    description: "Notify the CSMS about the EV’s charging needs."
  },
  {
    name: "NotifyEVChargingSchedule",
    description: "Notify the CSMS about the EV’s charging schedule."
  },
  {
    name: "NotifyEvent",
    description: "Notify the CSMS about a significant event at the charge point."
  },
  {
    name: "NotifyMonitoringReport",
    description: "Send a monitoring report to the CSMS."
  },
  {
    name: "NotifyReport",
    description: "Send a custom report to the CSMS."
  },
  {
    name: "PublishFirmware",
    description: "Publish firmware updates to the charge point."
  },
  {
    name: "PublishFirmwareStatusNotification",
    description: "Notify the CSMS about the status of a firmware publication."
  },
  {
    name: "ReportChargingProfiles",
    description: "Send active charging profiles to the CSMS."
  },
  {
    name: "RequestStartTransaction",
    description: "Request the start of a charging session."
  },
  {
    name: "RequestStopTransaction",
    description: "Request the end of a charging session."
  },
  {
    name: "ReservationStatusUpdate",
    description: "Notify the CSMS about the status of a reservation."
  },
  {
    name: "ReserveNow",
    description: "Reserve a charge point for a future charging session."
  },
  {
    name: "Reset",
    description: "Reset the charge point (soft or hard reset)."
  },
  {
    name: "SecurityEventNotification",
    description: "Notify the CSMS of a security-related event."
  },
  {
    name: "SendLocalList",
    description: "Send a local authorization list to the charge point."
  },
  {
    name: "SetChargingProfile",
    description: "Set a charging profile for a charge point."
  },
  {
    name: "SetDisplayMessage",
    description: "Send a message to be displayed on the charge point's screen."
  },
  {
    name: "SetMonitoringBase",
    description: "Set the base for monitoring parameters at the charge point."
  },
  {
    name: "SetMonitoringLevel",
    description: "Set the monitoring level for specific variables."
  },
  {
    name: "SetNetworkProfile",
    description: "Set the network configuration profile for the charge point."
  },
  {
    name: "SetVariableMonitoring",
    description: "Set monitoring rules for specific variables at the charge point."
  },
  {
    name: "SetVariables",
    description: "Set values for specific variables at the charge point."
  },
  {
    name: "SignCertificate",
    description: "Sign a security certificate."
  },
  {
    name: "StatusNotification",
    description: "Notify the CSMS of the current status of a charge point."
  },
  {
    name: "TransactionEvent",
    description: "Notify the CSMS about a transaction event (start, update, or stop)."
  },
  {
    name: "TriggerMessage",
    description: "Request a specific message from the charge point."
  },
  {
    name: "UnlockConnector",
    description: "Unlock a connector at the charge point."
  },
  {
    name: "UnpublishFirmware",
    description: "Unpublish a firmware version from the charge point."
  },
  {
    name: "UpdateFirmware",
    description: "Update the firmware on the charge point."
  }
]

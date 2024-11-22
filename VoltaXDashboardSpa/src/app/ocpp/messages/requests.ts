export const OCPPActions = [
  {
    name: "Authorize",
    description: "Authorize a user to start a charging session.",
    schemaPath : "/assets/schemas/AuthorizeRequest.json",
    
  },
  {
    name: "BootNotification",
    description: "Sent when a charge point boots or reboots to notify the CSMS.",
    schemaPath : "/assets/schemas/BootNotificationRequest.json"
  },
  {
    name: "CancelReservation",
    description: "Cancel a previously made reservation for a charge point.",
    schemaPath : "/assets/schemas/CancelReservationRequest.json"
  },
  {
    name: "CertificateSigned",
    description: "Notify the charge point that a new certificate has been signed.",
    schemaPath : "/assets/schemas/CertificateSignedRequest.json"
  },
  {
    name: "ChangeAvailability",
    description: "Change the availability status of a charge point (inoperative/operative).",
    schemaPath : "/assets/schemas/ChangeAvailabilityRequest.json"
  },
  {
    name: "ClearCache",
    description: "Clear the local cache of the charge point.",
    schemaPath : "/assets/schemas/ClearCacheRequest.json"
  },
  {
    name: "ClearChargingProfile",
    description: "Clear a previously set charging profile for a charge point.",
    schemaPath : "/assets/schemas/ClearChargingProfileRequest.json"
  },
  {
    name: "ClearDisplayMessage",
    description: "Clear a message currently displayed on the charge point's screen.",
    schemaPath : "/assets/schemas/ClearDisplayMessageRequest.json"
  },
  {
    name: "ClearedChargingLimit",
    description: "Notify that a previously set charging limit has been cleared.",
    schemaPath : "/assets/schemas/ClearedChargingLimitRequest.json"
  },
  {
    name: "ClearVariableMonitoring",
    description: "Clear monitoring settings for specific variables at a charge point.",
    schemaPath : "/assets/schemas/ClearVariableMonitoringRequest.json"
  },
  {
    name: "CostUpdated",
    description: "Notify the charge point of updated energy costs.",
    schemaPath : "/assets/schemas/CostUpdatedRequest.json"
  },
  {
    name: "CustomerInformation",
    description: "Send customer-specific information to the CSMS.",
    schemaPath : "/assets/schemas/CustomerInformationRequest.json"
  },
  {
    name: "DataTransfer",
    description: "Send proprietary data between the charge point and the CSMS.",
    schemaPath : "/assets/schemas/DataTransferRequest.json"
  },
  {
    name: "DeleteCertificate",
    description: "Delete an installed security certificate from the charge point.",
    schemaPath : "/assets/schemas/DeleteCertificateRequest.json"
  },
  {
    name: "FirmwareStatusNotification",
    description: "Notify the CSMS of the status of a firmware update.",
    schemaPath : "/assets/schemas/FirmwareStatusNotificationRequest.json"
  },
  {
    name: "Get15118EVCertificate",
    description: "Retrieve an ISO 15118 compliant EV certificate from the CSMS.",
    schemaPath : "/assets/schemas/Get15118EVCertificateRequest.json"
  },
  {
    name: "GetBaseReport",
    description: "Request a basic report from the charge point.",
    schemaPath : "/assets/schemas/GetBaseReportRequest.json"
  },
  {
    name: "GetCertificateStatus",
    description: "Request the status of an installed certificate.",
    schemaPath : "/assets/schemas/GetCertificateStatusRequest.json"
  },
  {
    name: "GetChargingProfiles",
    description: "Retrieve active charging profiles from the charge point.",
    schemaPath : "/assets/schemas/GetChargingProfilesRequest.json"
  },
  {
    name: "GetCompositeSchedule",
    description: "Request a charging schedule for a specified time period.",
    schemaPath : "/assets/schemas/GetCompositeScheduleRequest.json"
  },
  {
    name: "GetDisplayMessages",
    description: "Retrieve messages currently displayed or scheduled to be displayed.",
    schemaPath : "/assets/schemas/GetDisplayMessagesRequest.json"
  },
  {
    name: "GetInstalledCertificateIds",
    description: "Retrieve the list of installed certificates at the charge point.",
    schemaPath : "/assets/schemas/GetInstalledCertificateIdsRequest.json"
  },
  {
    name: "GetLocalListVersion",
    description: "Retrieve the version of the local authorization list.",
    schemaPath : "/assets/schemas/GetLocalListVersionRequest.json"
  },
  {
    name: "GetLog",
    description: "Request logs from the charge point.",
    schemaPath : "/assets/schemas/GetLogRequest.json"
  },
  {
    name: "GetMonitoringReport",
    description: "Request a report of all monitored variables.",
    schemaPath : "/assets/schemas/GetMonitoringReportRequest.json"
  },
  {
    name: "GetReport",
    description: "Request detailed reports from the charge point.",
    schemaPath : "/assets/schemas/GetReportRequest.json"
  },
  {
    name: "GetTransactionStatus",
    description: "Request the current status of a transaction.",
    schemaPath : "/assets/schemas/GetTransactionStatusRequest.json"
  },
  {
    name: "GetVariables",
    description: "Retrieve the current values of variables from the charge point.",
    schemaPath : "/assets/schemas/GetVariablesRequest.json"
  },
  {
    name: "Heartbeat",
    description: "Heartbeat message to check communication between charge point and CSMS.",
    schemaPath : "/assets/schemas/HeartbeatRequest.json"
  },
  {
    name: "InstallCertificate",
    description: "Install a new certificate on the charge point.",
    schemaPath : "/assets/schemas/InstallCertificateRequest.json"
  },
  {
    name: "LogStatusNotification",
    description: "Notify the CSMS of the current status of log uploads.",
    schemaPath : "/assets/schemas/LogStatusNotificationRequest.json"
  },
  {
    name: "MeterValues",
    description: "Send meter readings (energy usage data) from the charge point to the CSMS.",
    schemaPath : "/assets/schemas/MeterValuesRequest.json"
  },
  {
    name: "NotifyChargingLimit",
    description: "Notify the CSMS of the current charging limit.",
    schemaPath : "/assets/schemas/NotifyChargingLimitRequest.json"
  },
  {
    name: "NotifyCustomerInformation",
    description: "Notify the CSMS with customer-specific information.",
    schemaPath : "/assets/schemas/NotifyCustomerInformationRequest.json"
  },
  {
    name: "NotifyDisplayMessages",
    description: "Notify the CSMS about display messages at the charge point.",
    schemaPath : "/assets/schemas/NotifyDisplayMessagesRequest.json"
  },
  {
    name: "NotifyEVChargingNeeds",
    description: "Notify the CSMS about the EV’s charging needs.",
    schemaPath : "/assets/schemas/NotifyEVChargingNeedsRequest.json"
  },
  {
    name: "NotifyEVChargingSchedule",
    description: "Notify the CSMS about the EV’s charging schedule.",
    schemaPath : "/assets/schemas/NotifyEVChargingScheduleRequest.json"
  },
  {
    name: "NotifyEvent",
    description: "Notify the CSMS about a significant event at the charge point.",
    schemaPath : "/assets/schemas/NotifyEventRequest.json"
  },
  {
    name: "NotifyMonitoringReport",
    description: "Send a monitoring report to the CSMS.",
    schemaPath : "/assets/schemas/NotifyMonitoringReportRequest.json"
  },
  {
    name: "NotifyReport",
    description: "Send a custom report to the CSMS.",
    schemaPath : "/assets/schemas/NotifyReportRequest.json"
  },
  {
    name: "PublishFirmware",
    description: "Publish firmware updates to the charge point.",
    schemaPath : "/assets/schemas/PublishFirmwareRequest.json"
  },
  {
    name: "PublishFirmwareStatusNotification",
    description: "Notify the CSMS about the status of a firmware publication.",
    schemaPath : "/assets/schemas/PublishFirmwareStatusNotificationRequest.json"
  },
  {
    name: "ReportChargingProfiles",
    description: "Send active charging profiles to the CSMS.",
    schemaPath : "/assets/schemas/ReportChargingProfilesRequest.json"
  },
  {
    name: "RequestStartTransaction",
    description: "Request the start of a charging session.",
    schemaPath : "/assets/schemas/RequestStartTransactionRequest.json",
    requestable : true
  },
  {
    name: "RequestStopTransaction",
    description: "Request the end of a charging session.",
    schemaPath : "/assets/schemas/RequestStopTransactionRequest.json",
    requestable : true
  },
  {
    name: "ReservationStatusUpdate",
    description: "Notify the CSMS about the status of a reservation.",
    schemaPath : "/assets/schemas/ReservationStatusUpdateRequest.json"
  },
  {
    name: "ReserveNow",
    description: "Reserve a charge point for a future charging session.",
    schemaPath : "/assets/schemas/ReserveNowRequest.json"
  },
  {
    name: "Reset",
    description: "Reset the charge point (soft or hard reset).",
    schemaPath : "/assets/schemas/ResetRequest.json"
  },
  {
    name: "SecurityEventNotification",
    description: "Notify the CSMS of a security-related event.",
    schemaPath : "/assets/schemas/SecurityEventNotificationRequest.json"
  },
  {
    name: "SendLocalList",
    description: "Send a local authorization list to the charge point.",
    schemaPath : "/assets/schemas/SendLocalListRequest.json"
  },
  {
    name: "SetChargingProfile",
    description: "Set a charging profile for a charge point.",
    schemaPath : "/assets/schemas/SetChargingProfileRequest.json"
  },
  {
    name: "SetDisplayMessage",
    description: "Send a message to be displayed on the charge point's screen.",
    schemaPath : "/assets/schemas/SetDisplayMessageRequest.json"
  },
  {
    name: "SetMonitoringBase",
    description: "Set the base for monitoring parameters at the charge point.",
    schemaPath : "/assets/schemas/SetMonitoringBaseRequest.json"
  },
  {
    name: "SetMonitoringLevel",
    description: "Set the monitoring level for specific variables.",
    schemaPath : "/assets/schemas/SetMonitoringLevelRequest.json"
  },
  {
    name: "SetNetworkProfile",
    description: "Set the network configuration profile for the charge point.",
    schemaPath : "/assets/schemas/SetNetworkProfileRequest.json"
  },
  {
    name: "SetVariableMonitoring",
    description: "Set monitoring rules for specific variables at the charge point.",
    schemaPath : "/assets/schemas/SetVariableMonitoringRequest.json"
  },
  {
    name: "SetVariables",
    description: "Set values for specific variables at the charge point.",
    schemaPath : "/assets/schemas/SetVariablesRequest.json"
  },
  {
    name: "SignCertificate",
    description: "Sign a security certificate.",
    schemaPath : "/assets/schemas/SignCertificateRequest.json"
  },
  {
    name: "StatusNotification",
    description: "Notify the CSMS of the current status of a charge point.",
    schemaPath : "/assets/schemas/StatusNotificationRequest.json"
  },
  {
    name: "TransactionEvent",
    description: "Notify the CSMS about a transaction event (start, update, or stop).",
    schemaPath : "/assets/schemas/TransactionEventRequest.json"
  },
  {
    name: "TriggerMessage",
    description: "Request a specific message from the charge point.",
    schemaPath : "/assets/schemas/TriggerMessageRequest.json"
  },
  {
    name: "UnlockConnector",
    description: "Unlock a connector at the charge point.",
    schemaPath : "/assets/schemas/UnlockConnectorRequest.json",
    requestable : true
  },
  {
    name: "UnpublishFirmware",
    description: "Unpublish a firmware version from the charge point.",
    schemaPath : "/assets/schemas/UnpublishFirmwareRequest.json"
  },
  {
    name: "UpdateFirmware",
    description: "Update the firmware on the charge point.",
    schemaPath : "/assets/schemas/UpdateFirmwareRequest.json"
  }
]

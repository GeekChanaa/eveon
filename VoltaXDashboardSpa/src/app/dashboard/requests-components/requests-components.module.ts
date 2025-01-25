import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { RequestStopTransactionComponent } from './request-stop-transaction/request-stop-transaction.component';
import { RequestStartTransactionComponent } from './request-start-transaction/request-start-transaction.component';
import { RequestUnlockConnectorComponent } from './request-unlock-connector/request-unlock-connector.component';
import { RequestGetBaseReportComponent } from './request-get-base-report/request-get-base-report.component';
import { RequestRefreshChargerComponentsComponent } from './request-refresh-charger-components/request-refresh-charger-components.component';
import { ChangeAvailabilityRequestComponent } from './change-availability-request/change-availability-request.component';
import { ClearCacheRequestComponent } from './clear-cache-request/clear-cache-request.component';
import { ClearDisplayMessageRequestComponent } from './clear-display-message-request/clear-display-message-request.component';
import { ClearVariableMonitoringRequestComponent } from './clear-variable-monitoring-request/clear-variable-monitoring-request.component';
import { GetDisplayMessagesRequestComponent } from './get-display-messages-request/get-display-messages-request.component';
import { GetLogRequestComponent } from './get-log-request/get-log-request.component';
import { GetMonitoringReportRequestComponent } from './get-monitoring-report-request/get-monitoring-report-request.component';
import { GetReportRequestComponent } from './get-report-request/get-report-request.component';
import { GetTransactionStatusRequestComponent } from './get-transaction-status-request/get-transaction-status-request.component';
import { GetVariablesRequestComponent } from './get-variables-request/get-variables-request.component';
import { InstallCertificateRequestComponent } from './install-certificate-request/install-certificate-request.component';
import { PublishFirmwareRequestComponent } from './publish-firmware-request/publish-firmware-request.component';
import { ResetRequestComponent } from './reset-request/reset-request.component';
import { SetDisplayMessageRequestComponent } from './set-display-message-request/set-display-message-request.component';
import { SetMonitoringBaseRequestComponent } from './set-monitoring-base-request/set-monitoring-base-request.component';
import { SetMonitoringLevelRequestComponent } from './set-monitoring-level-request/set-monitoring-level-request.component';
import { SetNetworkProfileRequestComponent } from './set-network-profile-request/set-network-profile-request.component';
import { SetVariableMonitoringRequestComponent } from './set-variable-monitoring-request/set-variable-monitoring-request.component';
import { SetVariablesRequestComponent } from './set-variables-request/set-variables-request.component';
import { UnpublishFirmwareRequestComponent } from './unpublish-firmware-request/unpublish-firmware-request.component';
import { UpdateFirmwareRequestComponent } from './update-firmware-request/update-firmware-request.component';

@NgModule({
    declarations: [
      RequestStopTransactionComponent,
      RequestStartTransactionComponent,
      RequestUnlockConnectorComponent,
      RequestGetBaseReportComponent,
      RequestRefreshChargerComponentsComponent,
      ChangeAvailabilityRequestComponent,
      ClearCacheRequestComponent,
      ClearDisplayMessageRequestComponent,
      ClearVariableMonitoringRequestComponent,
      GetDisplayMessagesRequestComponent,
      GetLogRequestComponent,
      GetMonitoringReportRequestComponent,
      GetReportRequestComponent,
      GetTransactionStatusRequestComponent,
      GetVariablesRequestComponent,
      InstallCertificateRequestComponent,
      PublishFirmwareRequestComponent,
      ResetRequestComponent,
      SetDisplayMessageRequestComponent,
      SetMonitoringBaseRequestComponent,
      SetMonitoringLevelRequestComponent,
      SetNetworkProfileRequestComponent,
      SetVariableMonitoringRequestComponent,
      SetVariablesRequestComponent,
      UnpublishFirmwareRequestComponent,
      UpdateFirmwareRequestComponent
    ],
    imports: [
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule
    ],
    exports: [
      RequestStopTransactionComponent,
      RequestStartTransactionComponent,
      RequestUnlockConnectorComponent,
      RequestGetBaseReportComponent,
      RequestRefreshChargerComponentsComponent,
      ChangeAvailabilityRequestComponent,
      ClearCacheRequestComponent,
      ClearDisplayMessageRequestComponent,
      ClearVariableMonitoringRequestComponent,
      GetDisplayMessagesRequestComponent,
      GetLogRequestComponent,
      GetMonitoringReportRequestComponent,
      GetReportRequestComponent,
      GetTransactionStatusRequestComponent,
      GetVariablesRequestComponent,
      InstallCertificateRequestComponent,
      PublishFirmwareRequestComponent,
      ResetRequestComponent,
      SetDisplayMessageRequestComponent,
      SetMonitoringBaseRequestComponent,
      SetMonitoringLevelRequestComponent,
      SetNetworkProfileRequestComponent,
      SetVariableMonitoringRequestComponent,
      SetVariablesRequestComponent,
      UnpublishFirmwareRequestComponent,
      UpdateFirmwareRequestComponent
    ]
  })
  export class RequestsComponentsModule { }
  
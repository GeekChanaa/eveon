import { HttpErrorResponse } from '@angular/common/http';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';

// OCPP statuses meaning the charger took the command into account.
const ACCEPTED_LIKE_STATUSES = ['Accepted', 'Scheduled', 'RebootRequired', 'Unlocked'];

const POPUP_DURATION = 4000;

/**
 * Shows the outcome of an OCPP command (POST /ocpp/<Category>/<Action>/<chargePointID>)
 * now that the API waits for the charger's answer.
 * Pass the success body (200 { message, status, response }) or the HttpErrorResponse.
 * Returns true when the charger accepted the command (callers emit successEvent only then).
 */
export function showOcppCommandFeedback(modalService: ActionModalService, result: any): boolean {
  if (result instanceof HttpErrorResponse) {
    let message : string;
    switch (result.status) {
      case 409:
        message = "The charge point is not connected.";
        break;
      case 504:
        message = "The charger did not answer in time.";
        break;
      case 502:
        message = "The charger returned an error: " + (result.error?.errorCode || "unknown");
        break;
      default:
        message = result.error?.message || "Something went wrong please contact your system administrator";
    }
    modalService.popup(ActionModalStatusEnum.Error, "Error !", message, POPUP_DURATION);
    return false;
  }

  const status : string | null | undefined = result?.status;
  if (status == null) {
    modalService.popup(ActionModalStatusEnum.Success, "Success !", result?.message || "Request Sent successfully !", POPUP_DURATION);
    return true;
  }
  if (ACCEPTED_LIKE_STATUSES.includes(status)) {
    modalService.popup(ActionModalStatusEnum.Success, "Success !", "Charger answered: " + status, POPUP_DURATION);
    return true;
  }
  modalService.popup(ActionModalStatusEnum.Warning, "Warning !", "Charger answered: " + status, POPUP_DURATION);
  return false;
}

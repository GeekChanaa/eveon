import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { VxTestingModule } from 'src/testing/vx-testing.module';
import { ActionModalService } from './action-modal.service';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let toast: ToastService;
  let popup: jasmine.Spy;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [VxTestingModule] });
    toast = TestBed.inject(ToastService);
    popup = spyOn(TestBed.inject(ActionModalService), 'popup');
  });

  it('shows translated messages (keys as is without translations)', () => {
    toast.success('alarms.toast.resolved');
    expect(popup).toHaveBeenCalledWith(ActionModalStatusEnum.Success, 'operator.toast.success', 'alarms.toast.resolved', 3000);
    toast.error();
    expect(popup).toHaveBeenCalledWith(ActionModalStatusEnum.Error, 'operator.toast.error', 'common.state.error', 5000);
  });

  it('shows the API message of a refused request and the fallback for server errors', () => {
    toast.apiError(new HttpErrorResponse({ status: 409, error: { error: 'The alarm is already closed.' } }));
    expect(popup.calls.mostRecent().args[2]).toBe('The alarm is already closed.');
    toast.apiError(new HttpErrorResponse({ status: 400, error: { title: 'One or more validation errors occurred.', errors: { Name: ['The Name field is required.'] } } }));
    expect(popup.calls.mostRecent().args[2]).toBe('The Name field is required.');
    toast.apiError(new HttpErrorResponse({ status: 500, error: 'Internal stack trace' }), 'alarms.toast.actionFailed');
    expect(popup.calls.mostRecent().args[2]).toBe('alarms.toast.actionFailed');
    toast.apiError(new HttpErrorResponse({ status: 0 }));
    expect(popup.calls.mostRecent().args[2]).toBe('common.state.error');
  });
});

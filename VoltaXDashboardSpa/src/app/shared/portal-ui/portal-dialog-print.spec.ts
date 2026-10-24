import { HttpErrorResponse } from '@angular/common/http';
import { Component, ViewChild } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { LanguageService } from 'src/_services/language.service';
import { VxConfirmDialogComponent } from './vx-confirm-dialog.component';
import { VxPrintHeaderComponent } from './vx-print.components';
import { VxDurationPipe, elapsedClock, fullName } from './portal-ui.module';
import { commandFeedback, feedbackClass } from './remote-command';

@Component({
  template: `
    <button id="opener" (click)="dialog.open()">Open</button>
    <vx-confirm-dialog #dialog title="Stop?" confirmLabel="Stop" tone="danger" (confirmed)="confirmed = confirmed + 1" (cancelled)="cancelled = cancelled + 1">
      <p class="extra">Charging stops.</p>
    </vx-confirm-dialog>`
})
class DialogHostComponent {
  @ViewChild('dialog') dialog!: VxConfirmDialogComponent;
  confirmed = 0;
  cancelled = 0;
}

describe('Portal dialogs, printing and command feedback', () => {
  describe('VxConfirmDialogComponent', () => {
    beforeEach(() => configureVxTest([DialogHostComponent, VxConfirmDialogComponent]));

    function open() {
      const fixture = TestBed.createComponent(DialogHostComponent);
      fixture.detectChanges();
      const element: HTMLElement = fixture.nativeElement;
      (element.querySelector('#opener') as HTMLButtonElement).click();
      fixture.detectChanges();
      return { fixture, host: fixture.componentInstance, element, dialog: element.querySelector('dialog') as HTMLDialogElement };
    }

    it('opens as a modal, labelled by its title, with the projected explanation', () => {
      const { host, dialog } = open();

      expect(host.dialog.isOpen).toBeTrue();
      expect(dialog.getAttribute('aria-labelledby')).toBe(dialog.querySelector('h2')!.id);
      expect(dialog.querySelector('.extra')?.textContent).toContain('Charging stops.');
      expect(dialog.classList).toContain('vx-dialog--danger');
      // The safe choice has the focus.
      expect(document.activeElement?.textContent?.trim()).toBe('common.actions.cancel');
    });

    it('closes and emits confirmed on confirm', () => {
      const { host, dialog } = open();

      (dialog.querySelector('[data-confirm]') as HTMLButtonElement).click();

      expect(host.confirmed).toBe(1);
      expect(host.cancelled).toBe(0);
      expect(host.dialog.isOpen).toBeFalse();
    });

    it('treats Escape as cancel', () => {
      const { host, dialog } = open();

      dialog.dispatchEvent(new Event('cancel', { cancelable: true }));

      expect(host.cancelled).toBe(1);
      expect(host.confirmed).toBe(0);
      expect(host.dialog.isOpen).toBeFalse();
    });
  });

  describe('VxPrintHeaderComponent', () => {
    beforeEach(() => configureVxTest([VxPrintHeaderComponent]));

    it('prints the title, the holder and the partner name when there is no logo', () => {
      const fixture = TestBed.createComponent(VxPrintHeaderComponent);
      Object.assign(fixture.componentInstance, { title: 'Sessions', holder: 'Ev Driver', partnerName: 'Atlas Energy' });
      fixture.detectChanges();
      const text = (fixture.nativeElement as HTMLElement).textContent!;

      expect(text).toContain('Sessions');
      expect(text).toContain('Ev Driver');
      expect(text).toContain('Atlas Energy');
      expect(text).toContain('portal.print.printedAt');
    });

    it('dates the page when it is printed, not when it was opened', () => {
      const header = TestBed.createComponent(VxPrintHeaderComponent).componentInstance;
      header.printedAt = new Date(2000, 0, 1);

      window.dispatchEvent(new Event('beforeprint'));

      expect(header.printedAt.getFullYear()).toBeGreaterThan(2000);
    });
  });

  describe('formatting helpers', () => {
    it('shows a running clock', () => {
      expect(elapsedClock(0)).toBe('0:00');
      expect(elapsedClock(65_000)).toBe('1:05');
      expect(elapsedClock(3_725_000)).toBe('1:02:05');
      expect(elapsedClock(-5000)).toBe('0:00');
    });

    it('writes durations in minutes or hours', async () => {
      await configureVxTest([]);
      const language = TestBed.inject(LanguageService);
      spyOn(language, 't').and.callFake((key: string, params?: Record<string, unknown>) => `${key}:${JSON.stringify(params)}`);
      const pipe = new VxDurationPipe(language);

      expect(pipe.transform(44.6)).toBe('portal.duration.minutes:{"m":45}');
      expect(pipe.transform(125)).toBe('portal.duration.hours:{"h":2,"m":"05"}');
      expect(pipe.transform(null)).toBe('—');
    });

    it('names a person for printed headers', () => {
      expect(fullName({ firstName: 'Ev', lastName: 'Driver' })).toBe('Ev Driver');
      expect(fullName({ firstName: 'Ev', lastName: null })).toBe('Ev');
      expect(fullName({ firstName: '', lastName: '' })).toBeNull();
      expect(fullName(null)).toBeNull();
    });
  });

  describe('commandFeedback', () => {
    const error = (status: number, body: object = {}) => new HttpErrorResponse({ status, error: body });

    it('reports what the charger answered', () => {
      expect(commandFeedback({ message: '', status: 'Accepted' }, 'stop')).toEqual({ tone: 'success', key: 'portal.command.stop.done', params: {} });
      expect(commandFeedback({ message: '', status: 'Unlocked' }, 'unlock').key).toBe('portal.command.unlock.done');
      expect(commandFeedback({ message: '', status: 'Scheduled' }, 'reset').key).toBe('portal.command.reset.scheduled');
      expect(commandFeedback({ message: '', status: 'Rejected' }, 'reset')).toEqual({ tone: 'warning', key: 'portal.command.refused', params: { status: 'Rejected' } });
    });

    it('explains why a command could not be carried out', () => {
      expect(commandFeedback(error(409, { status: 'NotConnected' }), 'stop').key).toBe('portal.command.offline');
      expect(commandFeedback(error(409, { status: 'NotRunning' }), 'stop')).toEqual({ tone: 'warning', key: 'portal.command.notRunning', params: {} });
      expect(commandFeedback(error(404), 'stop').key).toBe('portal.command.notFound');
      expect(commandFeedback(error(504), 'reset').key).toBe('portal.command.timeout');
      expect(commandFeedback(error(502, { errorCode: 'NotImplemented' }), 'reset').params).toEqual({ code: 'NotImplemented' });
      expect(commandFeedback(error(400, { status: 'NotSupportedByProtocol' }), 'reset').key).toBe('portal.command.notSupported');
      expect(commandFeedback(error(500), 'reset').key).toBe('portal.command.failed');
      expect(feedbackClass('warning')).toBe('vx-alert--warning');
    });
  });
});

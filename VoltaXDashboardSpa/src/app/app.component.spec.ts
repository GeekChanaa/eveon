import { TestBed } from '@angular/core/testing';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(() => configureVxTest([AppComponent]));

  it('creates the application shell with its router outlet', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance).toBeTruthy();
    expect((fixture.nativeElement as HTMLElement).querySelector('router-outlet')).not.toBeNull();
  });
});

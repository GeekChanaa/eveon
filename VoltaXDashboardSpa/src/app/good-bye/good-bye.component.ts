import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';

// Keep in step with $redirect in good-bye.component.sass (the progress bar).
const REDIRECT_DELAY = 5000;

@Component({
  selector: 'app-good-bye',
  templateUrl: './good-bye.component.html',
  styleUrls: ['./good-bye.component.sass']
})
export class GoodByeComponent implements OnInit, OnDestroy {
  private redirectTimer?: ReturnType<typeof setTimeout>;

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.redirectTimer = setTimeout(() => this.router.navigate(['/auth/login']), REDIRECT_DELAY);
  }

  // "Sign in now" leaves early; the pending redirect must not fire afterwards.
  ngOnDestroy(): void {
    clearTimeout(this.redirectTimer);
  }
}

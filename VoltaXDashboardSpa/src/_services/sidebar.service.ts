import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

const COLLAPSED_KEY = 'sidebarCollapsed';
const COLLAPSED_CLASS = 'sidebar-collapsed';

@Injectable({
  providedIn: 'root'
})
export class SidebarService {

  // Desktop rail mode: icons only, labels hidden.
  private collapsed = new BehaviorSubject<boolean>(localStorage.getItem(COLLAPSED_KEY) === 'on');
  collapsed$ = this.collapsed.asObservable();

  // Mobile / tablet drawer.
  private opened = new BehaviorSubject<boolean>(false);
  opened$ = this.opened.asObservable();

  constructor() {
    this.applyCollapsedClass(this.collapsed.value);
  }

  get isCollapsed(): boolean {
    return this.collapsed.value;
  }

  get isOpened(): boolean {
    return this.opened.value;
  }

  toggleCollapsed(): void {
    this.setCollapsed(!this.collapsed.value);
  }

  setCollapsed(value: boolean): void {
    this.collapsed.next(value);
    localStorage.setItem(COLLAPSED_KEY, value ? 'on' : 'off');
    this.applyCollapsedClass(value);
  }

  toggleOpened(): void {
    this.opened.next(!this.opened.value);
  }

  open(): void {
    this.opened.next(true);
  }

  close(): void {
    this.opened.next(false);
  }

  // The class lives on <body> so the fixed .sidebar, .header and the
  // padded .page shell can all react from CSS without extra wiring.
  private applyCollapsedClass(value: boolean): void {
    document.body.classList.toggle(COLLAPSED_CLASS, value);
  }
}

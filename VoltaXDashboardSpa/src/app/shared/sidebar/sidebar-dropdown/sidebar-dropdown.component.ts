import { Component, Input, OnChanges, OnDestroy, OnInit, SimpleChanges } from '@angular/core';
import { Subscription } from 'rxjs';
import { SidebarService } from 'src/_services/sidebar.service';

/**
 * A collapsible group in the sidebar.
 *
 * Two different states used to share the single `active` class, which is why the
 * highlight never showed:
 *  - `expanded` — the group is unfolded, which the stylesheet reads as
 *    `.sidebar__item_dropdown.active .sidebar__body { display: block }`;
 *  - `active` — one of the routes inside is the current one, which the stylesheet
 *    reads as `.sidebar__top.active .sidebar__head`.
 *
 * They are kept apart here: the outer element carries the fold state, `.sidebar__top`
 * carries the route state. A group that owns the current route also unfolds itself, so
 * the selected child is visible without a click.
 */
@Component({
  selector: 'app-sidebar-dropdown',
  templateUrl: './sidebar-dropdown.component.html',
  styleUrls: ['./sidebar-dropdown.component.sass']
})
export class SidebarDropdownComponent implements OnInit, OnChanges, OnDestroy {
  @Input() active : boolean = false;
  @Input() title : string = "";
  @Input() icon : string = "";
  @Input() iconID : string = "";

  /** Fold state. Independent from `active`. */
  expanded : boolean = false;

  /** Icon rail on desktop: labels and the nested links are hidden. */
  collapsed : boolean = false;

  private subscriptions = new Subscription();

  constructor(private _sidebarService : SidebarService) { }

  ngOnInit() {
    this.subscriptions.add(
      this._sidebarService.collapsed$.subscribe(value => {
        this.collapsed = value;
        // Leaving the rail: reopen the group that owns the current route.
        if (!value && this.active) this.expanded = true;
      })
    );

    this.expanded = this.active;
  }

  ngOnChanges(changes: SimpleChanges) {
    // A navigation into this group unfolds it; navigating away leaves it as the user
    // left it, so a group opened by hand does not slam shut under the cursor.
    if (changes['active'] && this.active) {
      this.expanded = true;
    }
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }

  toggle(event: Event) {
    event.stopPropagation();
    event.preventDefault();

    // In the rail there is nowhere to draw the children, so the first click widens the
    // sidebar and unfolds the group in one go.
    if (this.collapsed) {
      this._sidebarService.setCollapsed(false);
      this.expanded = true;
      return;
    }

    this.expanded = !this.expanded;
  }
}

import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { RouterModule } from '@angular/router';
import { SidebarComponent } from './sidebar/sidebar.component';
import { NavbarComponent } from './navbar/navbar.component';
import { SharedComponent } from './shared.component';
import { SidebarDropdownComponent } from './sidebar/sidebar-dropdown/sidebar-dropdown.component';
import { SidebarItemComponent } from './sidebar/sidebar-item/sidebar-item.component';
import { SidebarLinkComponent } from './sidebar/sidebar-link/sidebar-link.component';
import { NavbarNotificationsComponent } from './navbar/navbar-notifications/navbar-notifications.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NavbarSearchComponent } from './navbar/navbar-search/navbar-search.component';

@NgModule({
  declarations: [
      SidebarComponent,
      NavbarComponent,
      SharedComponent,
      SidebarItemComponent,
      SidebarDropdownComponent,
      SidebarLinkComponent,
      NavbarNotificationsComponent,
      NavbarSearchComponent
   ],
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule
  ],
  exports: [
    SidebarComponent,
    NavbarComponent
  ],
  providers: [],
})
export class SharedModule { }

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

@NgModule({
  declarations: [
      SidebarComponent,
      NavbarComponent,
      SharedComponent,
      SidebarItemComponent,
    SidebarDropdownComponent,
    SidebarLinkComponent
   ],
  imports: [
    CommonModule,
    RouterModule,
  ],
  exports: [
    SidebarComponent,
    NavbarComponent
  ],
  providers: [],
})
export class SharedModule { }

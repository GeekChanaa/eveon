import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { PermissionsRoutingModule } from './permissions-routing.module';
import { PermissionsComponent } from './permissions.component';
import { PermissionsListComponent } from './permissions-list/permissions-list.component';
import { PermissionComponent } from './permission/permission.component';
import { PermissionRolesComponent } from './permission/permission-roles/permission-roles.component';


@NgModule({
  declarations: [
    PermissionsComponent,
    PermissionsListComponent,
    PermissionComponent,
    PermissionRolesComponent
  ],
  imports: [
      AtomsModule,
      ReactiveFormsModule,
      CommonModule,
      SharedModule,
      PermissionsRoutingModule,
      FormsModule,
  ],
})
export class PermissionsModule { }
  
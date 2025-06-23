import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { RolesRoutingModule } from './roles-routing.module';
import { CreateRoleComponent } from './create-role/create-role.component';
import { RoleComponent } from './role/role.component';
import { RolesListComponent } from './roles-list/roles-list.component';
import { RolesComponent } from './roles.component';


@NgModule({
  declarations: [
    CreateRoleComponent,
    RoleComponent,
    RolesListComponent,
    RolesComponent
  ],
  imports: [
      AtomsModule,
      ReactiveFormsModule,
      CommonModule,
      SharedModule,
      RolesRoutingModule,
      FormsModule,
  ],
})
export class RolesModule { }
  
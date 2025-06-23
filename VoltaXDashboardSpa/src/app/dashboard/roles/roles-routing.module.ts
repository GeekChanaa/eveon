import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RolesListComponent } from './roles-list/roles-list.component';
import { CreateRoleComponent } from './create-role/create-role.component';
import { RoleComponent } from './role/role.component';

const routes: Routes = [
  {
    path: "",
    component: RolesListComponent
  },
  {
    path: "create",
    component: CreateRoleComponent
  },
  {
    path: ":id",
    component: RoleComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class RolesRoutingModule { }

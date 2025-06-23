import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PermissionsListComponent } from './permissions-list/permissions-list.component';
import { PermissionComponent } from './permission/permission.component';

const routes: Routes = [
  {
    path: "",
    component: PermissionsListComponent
  },
  {
    path: ":id",
    component: PermissionComponent
  },
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PermissionsRoutingModule { }

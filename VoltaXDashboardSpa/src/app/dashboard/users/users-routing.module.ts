import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { UsersListComponent } from './users-list/users-list.component';
import { CreateUserComponent } from './create-user/create-user.component';
import { UserComponent } from './user/user.component';
const routes: Routes = [
  {
    path: "",
    component: UsersListComponent
  },
  {
    path: "create",
    component: CreateUserComponent
  },
  {
    path: ":id",
    component: UserComponent
  },
  
  
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class UsersRoutingModule { }

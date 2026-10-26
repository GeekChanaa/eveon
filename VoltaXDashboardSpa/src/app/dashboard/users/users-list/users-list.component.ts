import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ListFilterDef } from 'src/_models/list-filter';
import { UserListDto } from 'src/_models/_dtos/user-list-dto';
import { RoleService } from 'src/_services/roles/role.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-users-list',
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.sass']
})
export class UsersListComponent implements OnInit {

  fields: string[] = [];
  filterDefs: ListFilterDef[] = this._buildFilterDefs([]);
  

  user: UserListDto = {
    id: 0,
    firstName: '',
    lastName: '',
    email: '',
    gender: '',
    city: '',
    car: '',
    birthday: new Date(),
    phone: '',
    partnerName: '',
    isEmailVerified: false,
    isPhoneNumberVerified: false,
    role: '',
    suspendedAt: '',
    fullName: ''
  }

  // Constructor
  constructor(
    private _userService: UserService,
    private _roleService: RoleService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
    // Roles are configurable: offer the ones that exist.
    this._roleService.getAllRoles().subscribe({
      next: (roles: { name: string }[]) => this.filterDefs = this._buildFilterDefs((roles ?? []).map(r => r.name)),
      error: () => {}
    });
  }

  /** The role filter exists from the start (a filter remembered from earlier applies on the first load); its choices arrive with the roles. */
  private _buildFilterDefs(roles: string[]): ListFilterDef[] {
    return [
      { key: 'role', field: 'Role-Name', label: 'Role', type: 'multiselect', options: roles.map(name => ({ value: name, label: name })) },
      { key: 'city', label: 'City', type: 'text' },
      { key: 'partner', field: 'Partner-Name', label: 'Partner', type: 'text' },
      { key: 'isEmailVerified', label: 'Email verified', type: 'boolean' },
      { key: 'isPhoneNumberVerified', label: 'Phone verified', type: 'boolean' },
      { key: 'suspended', field: 'SuspendedAt', label: 'Suspended', type: 'boolean', presence: true },
      { key: 'createdAt', label: 'Joined', type: 'date-range' },
    ];
  }

  getUsersObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._userService.getAllUsers(currentPage, itemsPerPage, itemParams);
  deleteUserObservable = (id : number) => this._userService.deleteById(id);
  updateUserObservable = (id : number, model : any) => this._userService.edit(id, model);

  private _getItemFields() {
    if (!this.user || this.user == undefined) {
      return;
    }
    Object.keys(this.user ?? {}).forEach((element: string) => {
      if (typeof this.user?.[element] == "object" && this.user?.[element] != null && this.user?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.user?.[element] != "object") this.fields.push(element);
    });
  }
}

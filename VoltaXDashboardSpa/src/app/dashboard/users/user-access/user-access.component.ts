import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { AccessService } from 'src/_services/access.service';
import { environment } from 'src/environments/environment';
@Component({ selector: 'app-user-access', standalone: true, imports: [CommonModule, FormsModule],
  templateUrl: './user-access.component.html', styleUrls: ['./user-access.component.sass'] })
export class UserAccessComponent implements OnInit {
  @Input() userId = 0;
  @Input() roleId = 0;
  @Input() management = false;
  @Output() roleChanged = new EventEmitter<any>();
  roles: any[] = [];
  permissions: any[] = [];
  selectedRoleId = 0;
  selected = new Set<number>();
  creating = false;
  roleName = '';
  loading = true;
  saving = false;
  message = '';
  error = '';
  filter = '';
  roleFilter = '';
  readonly baseUrl = environment.apiUrl + '/api';
  constructor(public access: AccessService, private http: HttpClient) {}
  ngOnInit(): void { this.selectedRoleId = this.roleId; this.load(); }
  get selectedRole() { return this.roles.find(role => role.id === this.selectedRoleId); }
  get protectedRole(): boolean { return !this.creating && this.selectedRole?.name === 'Admin'; }
  get filteredRoles(): any[] { return this.roles.filter(role => role.name.toLowerCase().includes(this.roleFilter.toLowerCase())); }
  get dirty(): boolean {
    if (this.creating) return this.roleName.length > 0 || this.selected.size > 0;
    if (this.protectedRole) return false;
    const original: number[] = this.selectedRole?.permissionIds || [];
    return original.length !== this.selected.size || original.some(id => !this.selected.has(id));
  }
  chooseRole(id: number): void { if (this.saving || this.dirty) return; this.selectedRoleId = id; this.selectRole(); }
  get groups(): { name: string; permissions: any[] }[] {
    const groups = new Map<string, any[]>();
    this.permissions.filter(p => this.label(p.name).toLowerCase().includes(this.filter.toLowerCase())).forEach(p => {
      const feature = p.name.replace(/^(View|Create|Edit|Delete)/, '');
      const name = /^(AccessDashboard|ViewDashboard|ViewStatisticsPage|ViewDocumentation|OperateChargePoints)$/.test(p.name) ? 'Dashboard & operations' : this.label(feature);
      groups.set(name, [...(groups.get(name) || []), p]);
    });
    return Array.from(groups, ([name, permissions]) => ({ name, permissions }));
  }
  // `groups` builds new objects on every check; without these the checkboxes are recreated mid-click and lose their change event.
  trackGroup(_: number, group: { name: string }): string { return group.name; }
  trackPermission(_: number, permission: any): number { return permission.id; }
  label(name: string): string { return name.replace(/([a-z])([A-Z])/g, '$1 $2'); }
  load(successMessage = ''): void {
    this.loading = true; this.error = '';
    forkJoin({ roles: this.http.get<any[]>(this.baseUrl + '/role/GetAllRoles'), permissions: this.http.get<any[]>(this.baseUrl + '/permission/GetAllPermissions') }).subscribe({
      next: data => { this.roles = data.roles; this.permissions = data.permissions;
        if (this.management && !this.selectedRoleId) this.selectedRoleId = this.roles[0]?.id || 0;
        this.loading = false; this.selectRole(); this.message = successMessage; },
      error: () => { this.loading = false; this.error = 'Could not load roles and permissions.'; }
    });
  }
  selectRole(): void { this.creating = false; this.message = ''; this.error = ''; this.filter = ''; this.selected = new Set(this.selectedRole?.name === 'Admin' ? this.permissions.map(p => p.id) : this.selectedRole?.permissionIds || []); }
  newRole(): void { this.creating = true; this.roleName = ''; this.selected = new Set(); this.message = ''; }
  toggle(permission: any, checked: boolean): void {
    const names = new Map(this.permissions.map(p => [p.name, p.id]));
    if (checked) {
      this.selected.add(permission.id);
      const dashboard = names.get('AccessDashboard'); if (dashboard) this.selected.add(dashboard);
      const view = names.get(permission.name.replace(/^(Create|Edit|Delete)/, 'View')); if (view) this.selected.add(view);
      if (permission.name === 'OperateChargePoints' && names.has('ViewChargePoints')) this.selected.add(names.get('ViewChargePoints')!);
    } else {
      this.selected.delete(permission.id);
      if (permission.name === 'AccessDashboard') this.selected.clear();
      if (permission.name.startsWith('View')) this.permissions.filter(p => /^(Create|Edit|Delete)/.test(p.name) && p.name.replace(/^(Create|Edit|Delete)/, 'View') === permission.name).forEach(p => this.selected.delete(p.id));
      if (permission.name === 'ViewChargePoints' && names.has('OperateChargePoints')) this.selected.delete(names.get('OperateChargePoints')!);
    }
  }
  savePermissions(): void {
    if (this.saving || this.protectedRole || this.creating && this.roleName.trim().length < 2) return;
    this.saving = true; this.error = '';
    const request = this.creating ? this.http.post(this.baseUrl + '/role/CreateRole', { name: this.roleName.trim(), permissions: [...this.selected] })
      : this.http.put(this.baseUrl + '/role/UpdateRolePermissions/' + this.selectedRoleId, [...this.selected]);
    request.subscribe({ next: (result: any) => { this.saving = false; if (result?.id) this.selectedRoleId = result.id; this.load('Role saved. Permissions take effect immediately.'); },
      error: error => { this.saving = false; this.error = error.error?.message || 'Could not save the role.'; } });
  }
  assignRole(): void {
    if (this.saving || this.creating || !this.selectedRoleId) return;
    this.saving = true; this.error = '';
    this.access.assignRole(this.userId, this.selectedRoleId).subscribe({ next: result => { this.saving = false; this.roleId = result.roleId; this.roleChanged.emit(result); this.message = 'User role updated.'; },
      error: error => { this.saving = false; this.error = error.error?.message || 'Could not change the user role.'; } });
  }
}

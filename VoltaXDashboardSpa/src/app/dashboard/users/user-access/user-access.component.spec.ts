import { of } from 'rxjs';
import { UserAccessComponent } from './user-access.component';

describe('Role permission editor', () => {
  const roles = [{ id: 1, name: 'Admin', permissionIds: [] }, { id: 2, name: 'Customer', permissionIds: [] }, { id: 3, name: 'Operator', permissionIds: [1, 2] }];
  const permissions = [{ id: 1, name: 'AccessDashboard' }, { id: 2, name: 'ViewChargePoints' }, { id: 3, name: 'EditChargePoints' }, { id: 4, name: 'OperateChargePoints' }];
  let component: UserAccessComponent;
  let http: any;
  beforeEach(() => {
    http = { get: (url: string) => of(url.includes('/role/') ? roles : permissions), post: jasmine.createSpy().and.returnValue(of({ id: 3 })), put: jasmine.createSpy().and.returnValue(of(null)) };
    component = new UserAccessComponent({ isAdmin: true } as any, http);
    component.management = true;
    component.ngOnInit();
  });
  it('shows Admin full access but allows editing other roles', () => {
    expect(component.protectedRole).toBeTrue();
    expect(component.selected.size).toBe(permissions.length);
    component.savePermissions();
    expect(http.put).not.toHaveBeenCalled();
    component.chooseRole(2);
    expect(component.protectedRole).toBeFalse();
  });
  it('adds prerequisites and removes dependent operations when view access is removed', () => {
    component.chooseRole(2);
    component.toggle(permissions[2], true);
    component.toggle(permissions[3], true);
    expect([...component.selected].sort()).toEqual([1, 2, 3, 4]);
    component.toggle(permissions[1], false);
    expect([...component.selected]).toEqual([1]);
  });
  it('keeps unsaved permissions when another role is selected', () => {
    component.chooseRole(2);
    component.toggle(permissions[2], true);
    component.chooseRole(3);
    expect(component.selectedRoleId).toBe(2);
    expect(component.dirty).toBeTrue();
    component.selectRole();
    component.chooseRole(3);
    expect(component.selectedRoleId).toBe(3);
  });
  it('creates a role with its selected permissions and retains success feedback', () => {
    component.newRole();
    component.roleName = ' Station operator ';
    component.toggle(permissions[1], true);
    component.savePermissions();
    expect(http.post).toHaveBeenCalledWith(jasmine.stringMatching('/role/CreateRole$'), { name: 'Station operator', permissions: [2, 1] });
    expect(component.selectedRoleId).toBe(3);
    expect(component.message).toContain('Role saved');
    expect(component.creating).toBeFalse();
  });
});

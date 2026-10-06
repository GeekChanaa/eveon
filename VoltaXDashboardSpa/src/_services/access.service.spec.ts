import { of } from 'rxjs';
import { AccessInfo, AccessService, routePermission } from './access.service';
import { avatarUrl } from '../_helpers/avatar-url';

describe('Dashboard access boundaries', () => {
  it('maps reads, creation, edits and nested session routes separately', () => {
    expect(routePermission('/dashboard/users/42')).toBe('ViewUsers');
    expect(routePermission('/dashboard/users/create')).toBe('CreateUsers');
    expect(routePermission('/dashboard/charging-points/42/edit')).toBe('EditChargePoints');
    expect(routePermission('/dashboard/connector-realtime/42/charging-session/8')).toBe('ViewChargingSessions');
  });
  it('fails closed for unknown routes and reserves access management for admins', () => {
    expect(routePermission('/dashboard/new-feature')).toBe('__admin');
    expect(routePermission('/dashboard/roles')).toBe('__admin');
    expect(routePermission('/my-dashboard')).toBeNull();
  });
  it('requires dashboard access and forgets cached permissions when the token changes', () => {
    const tokens = { accessToken: 'first' };
    const info = { isAdmin: false, permissions: ['ViewUsers'] } as AccessInfo;
    const service = new AccessService({ get: () => of(info) } as any, tokens as any);
    service.load().subscribe();
    expect(service.canUrl('/dashboard/users')).toBeFalse();
    info.permissions.push('AccessDashboard');
    expect(service.canUrl('/dashboard/users')).toBeTrue();
    expect(service.canUrl('/dashboard/users/create')).toBeFalse();
    tokens.accessToken = 'second';
    expect(service.canUrl('/dashboard/users')).toBeFalse();
  });
  it('allows admins every dashboard route', () => {
    const service = new AccessService({ get: () => of({ isAdmin: true, permissions: [] }) } as any, { accessToken: 'admin' } as any);
    service.load().subscribe();
    expect(service.canUrl('/dashboard/new-feature')).toBeTrue();
  });
  it('supports hosted and uploaded avatars while rejecting unsafe URL schemes', () => {
    expect(avatarUrl('https://images.example/avatar.png')).toBe('https://images.example/avatar.png');
    expect(avatarUrl('/uploads/avatar.png')).toContain('/uploads/avatar.png');
    expect(avatarUrl('javascript:alert(1)')).toBeNull();
    expect(avatarUrl('//untrusted.example/avatar.png')).toBeNull();
    expect(avatarUrl(null)).toBeNull();
  });
});

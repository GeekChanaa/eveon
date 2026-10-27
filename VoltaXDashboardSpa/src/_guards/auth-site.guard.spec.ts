import { loginUrlFor } from './auth-site.guard';

describe('loginUrlFor', () => {
  it('sends a dashboard page to the customer sign in with the page to come back to', () => {
    expect(loginUrlFor('/dashboard/users?tab=2')).toBe('/auth/login?returnUrl=%2Fdashboard%2Fusers%3Ftab%3D2');
  });

  it('sends a partner page to the partner sign in', () => {
    expect(loginUrlFor('/partner-dashboard/stations')).toBe('/partner-auth/login?returnUrl=%2Fpartner-dashboard%2Fstations');
  });

  it('adds no return path for the home page or the auth pages', () => {
    expect(loginUrlFor('/')).toBe('/auth/login');
    expect(loginUrlFor('/auth/session')).toBe('/auth/login');
    expect(loginUrlFor('/partner-auth/login')).toBe('/partner-auth/login');
  });

  it('never returns to another site', () => {
    expect(loginUrlFor('//evil.com/x')).toBe('/auth/login');
    expect(loginUrlFor('https://evil.com')).toBe('/auth/login');
  });
});

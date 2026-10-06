import { environment } from 'src/environments/environment';
export function avatarUrl(value?: string | null): string | null {
  if (!value) return null;
  if (/^https?:\/\//i.test(value)) return value;
  if (/^[a-z][a-z0-9+.-]*:/i.test(value) || value.startsWith('//')) return null;
  return environment.apiStaticFilesUrl.replace(/\/$/, '') + '/' + value.replace(/^\/+/, '');
}

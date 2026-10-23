import { HttpParams, HttpResponse } from '@angular/common/http';

/** Saves a file returned by the API, using the server's file name when it sent one. */
export function saveDownload(response: HttpResponse<Blob>, fallbackName: string): void {
  const disposition = response.headers.get('content-disposition') ?? '';
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
  const name = match ? decodeURIComponent(match[1]) : fallbackName;
  const url = URL.createObjectURL(response.body!);
  const link = document.createElement('a');
  link.href = url;
  link.download = name;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/** Query parameters from a filter object, skipping empty values. */
export function toParams(filter: object): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(filter)) {
    if (value !== null && value !== undefined && value !== '') params = params.set(key, String(value));
  }
  return params;
}

/** Sends the browser to a payment provider: plain navigation, or an auto-submitted form for hosted payment pages. */
export function followCheckout(checkout: { method: 'GET' | 'POST'; url: string; fields?: Record<string, string> | null }): void {
  if (checkout.method === 'GET') {
    window.location.assign(checkout.url);
    return;
  }
  const form = document.createElement('form');
  form.method = 'POST';
  form.action = checkout.url;
  for (const [name, value] of Object.entries(checkout.fields ?? {})) {
    const input = document.createElement('input');
    input.type = 'hidden';
    input.name = name;
    input.value = value;
    form.appendChild(input);
  }
  document.body.appendChild(form);
  form.submit();
}

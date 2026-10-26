/**
 * Query string of a server-paged list request (GlobalParams on the API): page, size and the item params.
 * Arrays (SearchBy, FilterBy, FilterValue, FilterOperator) become repeated keys; every value is URL-encoded,
 * so dates with a zone offset ('+') and "in" lists ('|') reach the API intact.
 */
export function listQueryString(page?: number, itemsPerPage?: number, itemParams?: any): string {
  const parts: string[] = [];
  const add = (key: string, value: unknown) => parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`);

  if (page != null && itemsPerPage != null) {
    add('pageNumber', page);
    add('pageSize', itemsPerPage);
  }
  for (const key in itemParams ?? {}) {
    const value = itemParams[key];
    if (value == null) continue;
    if (Array.isArray(value)) value.forEach(v => add(key, v ?? ''));
    else add(key, value);
  }
  return parts.join('&');
}

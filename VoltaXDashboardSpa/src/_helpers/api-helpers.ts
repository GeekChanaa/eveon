import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaginatedResult } from '../_models/pagination';  // Adjust path
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ApiHelper<T> {
  constructor(private http: HttpClient) {}

  getAll(
    baseUrl : string,
    endpoint: string,
    page?: number,
    itemsPerPage?: number,
    itemParams?: any
  ): Observable<PaginatedResult<T[]>> {
    const paginatedResult = new PaginatedResult<T[]>();
    let params = new HttpParams();
    let queryString = '';

    // Add pagination params
    if (page != null && itemsPerPage != null) {
      params = params.append('pageNumber', page.toString());
      params = params.append('pageSize', itemsPerPage.toString());
    }

    // Add filters, search, etc.
    if (itemParams != null) {
      for (const key in itemParams) {
        if (itemParams[key] != null) {
          if (key === 'SearchBy') {
            for (let i = 0; i < itemParams.SearchBy.length; i++) {
              queryString += `&${key}=${itemParams.SearchBy[i]}`;
            }
          } else if (key === 'SearchValue') {
            queryString += `&${key}=${itemParams.SearchValue}`;
          } else if (key === 'FilterBy') {
            for (let i = 0; i < itemParams.FilterBy.length; i++) {
              queryString += `&${key}=${itemParams.FilterBy[i]}`;
            }
          } else if (key === 'FilterValue') {
            for (let i = 0; i < itemParams.FilterValue.length; i++) {
              queryString += `&${key}=${itemParams.FilterValue[i]}`;
            }
          } else {
            params = params.append(key, itemParams[key]);
          }
        }
      }
    }

    const url = `${baseUrl}${endpoint}?${params.toString()}${queryString}`;

    return this.http.get<T[]>(url, { observe: 'response' }).pipe(
      map((response: HttpResponse<T[]>) => {
        paginatedResult.result = response.body || [];
        const paginationHeader = response.headers.get('Pagination');
        if (paginationHeader != null) {
          paginatedResult.pagination = JSON.parse(paginationHeader);
        }
        return paginatedResult;
      })
    );
  }
}

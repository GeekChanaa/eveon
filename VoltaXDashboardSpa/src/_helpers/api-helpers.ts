import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaginatedResult } from '../_models/pagination';  // Adjust path
import { Injectable } from '@angular/core';
import { listQueryString } from './list-query';

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
    const url = `${baseUrl}${endpoint}?${listQueryString(page, itemsPerPage, itemParams)}`;

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

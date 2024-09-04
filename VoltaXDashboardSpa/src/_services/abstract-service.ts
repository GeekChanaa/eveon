import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaginatedResult } from '../_models/pagination';


export abstract class AbstractService<T> {

  constructor(
    protected _http: HttpClient, 
    protected actionUrl: string) {
  }

  // Http Options (defining some headers)
  // CONTENT-TYPE The MIME media type for JSON text is application/json. 
  // Defines the type of data we're sending to the server 
  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };

  getAll(page?: number, itemsPerPage?: number, itemParams?: any, endpoint: string = ""): Observable<PaginatedResult<T[]>> {
    const paginatedResult: PaginatedResult<T[]> | null = new PaginatedResult<T[]>();
    let params = new HttpParams();
    if (page != null && itemsPerPage != null) {
      params = params.append('pageNumber', page.toString());
      params = params.append('pageSize', itemsPerPage.toString());
    }

    // Other sorting and filtering params
    let queryString = "";
    if (itemParams != null) {
      for (const p in itemParams) {
        if (itemParams[p] != null)
        {
          if(p == "SearchBy"){
            for(var i =0 ;i < itemParams.SearchBy.length ;i++){
              queryString += "&" + p + "=" + itemParams.SearchBy[i];
            }
          }
          else if(p == "SearchValue"){
            queryString += "&" + p + "=" + itemParams.SearchValue;
          }
          else if(p == "FilterBy"){
            for(var i =0 ;i < itemParams.FilterBy.length ;i++){
              queryString += "&" + p + "=" + itemParams.FilterBy[i];
            }
          }
          else if(p == "FilterValue"){
            for(var i =0 ;i < itemParams.FilterValue.length ;i++){
              queryString += "&" + p + "=" + itemParams.FilterValue[i];
            }
          }
          else{
            params = params.append(p, itemParams[p]);
          }
        }
              
      }
    }
    const url = this.actionUrl + endpoint + "?" + params.toString() + queryString;
    return this._http.get<T[]>(url, { observe: 'response' })
        .pipe(
            map(response => {
            const paginationHeader = response.headers.get('Pagination');
            paginatedResult.result = response.body;
            if (paginationHeader != null) {
                paginatedResult.pagination = JSON.parse(paginationHeader);
            }
            return paginatedResult;
            })
        )
  }


  // Get Item by id
  getById(id: number): Observable<T> {
    return this._http.get<T>(this.actionUrl + id, this.httpOptions);
  }

  // Delete Item by id
  deleteById(id: number): Observable<T> {
    return this._http.delete<T>(this.actionUrl + id, this.httpOptions).pipe(map(response => {
      return response;
    }));
  }

  // Create item
  create(model: any): Observable<T> {
    return this._http.post<T>(this.actionUrl, model, this.httpOptions).pipe(map(response => {
      model = response;
      return response;
    }));
  }

  // Edit Item
  edit(id:number , model:any): Observable<T>{
    return this._http.put<T>(this.actionUrl+id, model, this.httpOptions).pipe(map(response => {
      model = response;
      return response;
    }));
  }

  // Count of items
  count(itemParams? : any) : Observable<number>{
    let params = new HttpParams();

    // Other sorting and filtering params
    if (itemParams != null) {
      for (const p in itemParams) {
        if (itemParams[p] != null)
        if(p == "FilterBy"){
          for(var i =0 ;i < itemParams.FilterBy.length ;i++)
          params = params.append(p,itemParams.FilterBy[i]);
        }
        else if(p == "FilterValue"){
          for(var i =0 ;i < itemParams.FilterValue.length ;i++)
          params = params.append(p,itemParams.FilterValue[i]);
        }
        else{
          params = params.append(p, itemParams[p]);
        }
      }
    }
    return this._http.get<number>(this.actionUrl+"countAll",{params : params});
  }

}
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaginatedResult } from '../_models/pagination';
import { MatSnackBar } from '@angular/material/snack-bar';


export abstract class AbstractService<T> {

  constructor(
    protected _http: HttpClient, 
    private _snackBar : MatSnackBar,
    protected actionUrl: string) {
  }

  // Http Options (defining some headers)
  // CONTENT-TYPE The MIME media type for JSON text is application/json. 
  // Defines the type of data we're sending to the server 
  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };

  getAll(page?: number, itemsPerPage?: number, itemParams?: any): Observable<PaginatedResult<T[]>> {
    const paginatedResult: PaginatedResult<T[]> | null = new PaginatedResult<T[]>();
    let params = new HttpParams();
    if (page != null && itemsPerPage != null) {
      params = params.append('pageNumber', page);
      params = params.append('pageSize', itemsPerPage);
    }

    // Other sorting and filtering params
    if (itemParams != null) {
      for (const p in itemParams) {
        if (itemParams[p] != null)
        {
          if(p == "filterBy"){
            for(var i =0 ;i < itemParams.filterBy.length ;i++){
              params = params.append(p,itemParams.filterBy[i]);
            }
          }
          else if(p == "filterValue"){
            for(var i =0 ;i < itemParams.filterValue.length ;i++){
              params = params.append(p,itemParams.filterValue[i]);
            }
          }
          else{
            params = params.append(p, itemParams[p]);
          }
        }
              
      }
    }
    return this._http.get<T[]>(this.actionUrl, { observe: 'response', params })
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
      this._snackBar.open("Item Deleted Succesfully","dismiss",{duration:2000});
      return response;
    }));
  }

  // Create item
  create(model: any): Observable<T> {
    return this._http.post<T>(this.actionUrl, model, this.httpOptions).pipe(map(response => {
      model = response;
      this._snackBar.open("Item Created Succesfully","dismiss",{duration:2000});
      return response;
    }));
  }

  // Edit Item
  edit(id:number , model:any): Observable<T>{
    return this._http.put<T>(this.actionUrl+id, model, this.httpOptions).pipe(map(response => {
      model = response;
      this._snackBar.open("Item Updated Succesfully","dismiss",{duration:2000});
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
        if(p == "filterBy"){
          for(var i =0 ;i < itemParams.filterBy.length ;i++)
          params = params.append(p,itemParams.filterBy[i]);
        }
        else if(p == "filterValue"){
          for(var i =0 ;i < itemParams.filterValue.length ;i++)
          params = params.append(p,itemParams.filterValue[i]);
        }
        else{
          params = params.append(p, itemParams[p]);
        }
      }
    }
    return this._http.get<number>(this.actionUrl+"count",{params : params});
  }

}
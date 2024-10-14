import { Injectable } from '@angular/core';
import * as jsf from 'json-schema-faker';
import { HttpClient } from '@angular/common/http'; 
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class SchemaService {

  constructor(private http: HttpClient) {}

  generateRandomObject(schemaPath: string): Observable<any> {
    return this.http.get(schemaPath).pipe(
      map(schema => {
        return jsf.JSONSchemaFaker.generate(schema);
      })
    );
  }
}

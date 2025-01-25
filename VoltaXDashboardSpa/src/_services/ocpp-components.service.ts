import { Injectable } from '@angular/core';
import { SystemReport } from 'src/_models/system-report';
import { environment } from 'src/environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class OcppComponentsService {
  baseUrl = environment.apiUrl+"/api/OcppComponents/";

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };

  constructor(private _http : HttpClient) {
  }

  getComponents(){
    return this._http.get<any>(this.baseUrl+"GetComponents", this.httpOptions);
  }

  getVariables(){
    return this._http.get<any>(this.baseUrl+"GetVariables", this.httpOptions);
  }

  getComponentByName(name : string){
    return this._http.get<any>(this.baseUrl+"getComponentByName/"+name, this.httpOptions);
  }

  getVariableByName(name : string){
    return this._http.get<any>(this.baseUrl+"getVariableByName/"+name, this.httpOptions);
  }

  getComponentInstances(name: string){
    return this._http.get<any>(this.baseUrl+"GetComponentInstances/"+name, this.httpOptions);
  }

  getComponentVariables(name: string){
    return this._http.get<any>(this.baseUrl+"GetComponentVariables/"+name, this.httpOptions);
  }
  
  getVariablesByComponentNameAndInstance(name: string, instance : string){
    return this._http.get<any>(this.baseUrl+"GetVariablesByComponentNameAndInstance/"+name+"/"+instance, this.httpOptions);
  }

  

}

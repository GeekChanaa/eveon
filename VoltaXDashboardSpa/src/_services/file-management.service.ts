import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class FileManagementService {

  // Base URL for the api
  baseUrl = environment.apiUrl + "/api/FileManagement/";

  // Constructor
  constructor(
    private http: HttpClient
  ) { }

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };


  // Upload Profile Picture of user
  uploadProfilePicture(formData : FormData){
    return this.http.post<any[]>(this.baseUrl + 'UploadProfilePicture', formData, { reportProgress: true, observe: 'events' });
  }

}
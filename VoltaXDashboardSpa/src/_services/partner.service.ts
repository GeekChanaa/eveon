import { Injectable } from '@angular/core';
import { Partner } from 'src/_models/partner';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PartnerService extends AbstractService<Partner>{

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/partner/");
  }

  baseUrl = environment.apiUrl+"/api/partner/";

  createPartner(partner : any){
    return this.http.post(this.baseUrl+"createPartner/",partner);
  }

  getAllPartners(page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetPartners");
  }

  getPartnerByID(partnerID : number){
    return this._http.get(this.baseUrl+"GetPartnerByID/"+partnerID)
  }

  getAllPartnersForSelect(){
    return this._http.get<any[]>(this.baseUrl+"GetAllPartnersForSelect/")
  }

  uploadPartnerLogo(formData : FormData, partnerId:  number){
    return this.http.post<any[]>(this.baseUrl + 'UploadPartnerLogo/'+partnerId, formData, { reportProgress: true, observe: 'events' });
  }
}

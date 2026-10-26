import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ListFilterDef } from 'src/_models/list-filter';
import { PartnerListDto } from 'src/_models/_dtos/partner-list-dto';
import { PartnerTypeEnum } from 'src/_models/_enums/partner-enum-type';
import { PartnerService } from 'src/_services/partner.service';

@Component({
  selector: 'app-partners-list',
  templateUrl: './partners-list.component.html',
  styleUrls: ['./partners-list.component.sass']
})
export class PartnersListComponent implements OnInit {

  fields: string[] = [];
  filterDefs: ListFilterDef[] = [
    { key: 'type', label: 'Partner type', type: 'multiselect', options: [
      { value: 'Vendor', label: 'Vendor' },
      { value: 'ServiceProvider', label: 'Service provider' },
      { value: 'Reseller', label: 'Reseller' },
      { value: 'Affiliate', label: 'Affiliate' },
      { value: 'Investor', label: 'Investor' },
      { value: 'Other', label: 'Other' },
    ] },
    { key: 'city', label: 'City', type: 'text' },
    { key: 'country', label: 'Country', type: 'text' },
    { key: 'commissionPerKwh', label: 'Commission per kWh (MAD)', type: 'number-range' },
    { key: 'createdAt', label: 'Created', type: 'date-range' },
  ];
  

  partner: PartnerListDto = {
    id: 0,
    partnerIdentificationNumber: '',
    name: '',
    description: '',
    type: PartnerTypeEnum.Vendor,
    address: '',
    logoUrl: '',
  }

  // Constructor
  constructor(
    private _partnerService: PartnerService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getPartnersObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._partnerService.getAllPartners(currentPage, itemsPerPage, itemParams);
  deletePartnerObservable = (id : number) => this._partnerService.deleteById(id);
  updatePartnerObservable = (id : number, model : any) => this._partnerService.edit(id, model);

  private _getItemFields() {
    if (!this.partner || this.partner == undefined) {
      return;
    }
    Object.keys(this.partner ?? {}).forEach((element: string) => {
      if (typeof this.partner?.[element] == "object" && this.partner?.[element] != null && this.partner?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.partner?.[element] != "object") this.fields.push(element);
    });
  }

}

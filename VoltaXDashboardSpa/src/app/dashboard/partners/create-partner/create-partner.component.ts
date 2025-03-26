import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { CityService } from 'src/_services/city.service';
import { CountryService } from 'src/_services/country.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { PartnerService } from 'src/_services/partner.service';

@Component({
  selector: 'app-create-partner',
  templateUrl: './create-partner.component.html',
  styleUrls: ['./create-partner.component.sass']
})
export class CreatePartnerComponent implements OnInit {

  form : FormGroup;
  partner : any = {};
  isLoading : boolean = false;
  
  ngAfterViewInit() {
  }

  ngOnInit() {
    this.getMoroccoCities();
  }

  opacity: number = 0;

  cardTypes : any = {};
  partnerTypes : any = {};
  cities : any[] = [];

  constructor(
    private _modalService:  ActionModalService,
    private _router : Router,
    private _partnerService: PartnerService,
    private _countryService: CountryService,
    private _cityService : CityService
  ) {
    this.form = new FormGroup({
      name : new FormControl(''),
      description : new FormControl(''),
      type : new FormControl('Vendor'),
      email : new FormControl(''),
      email2 : new FormControl(''),
      email3 : new FormControl(''),
      phone : new FormControl(''),
      phone2 : new FormControl(''),
      phone3 : new FormControl(''),
      city : new FormControl('Tangier'),
      country : new FormControl('Morocco'),
      address : new FormControl(''),
      taxIdentificationNumber : new FormControl(''),
      registrationNumber : new FormControl(''),
      bankAccountNumber : new FormControl(''),
    })
  }

  showSelect() {
    this.opacity = 1;
  }

  hideSelect() {
    this.opacity = 0;
  }

  getFormControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  getMoroccoCities(){
    this._cityService.getAllMoroccoCityNames().subscribe((data) => {
      this.cities = data;
    })
  }
  

  
  onSubmit(){
    this.isLoading = true;
    var partnerForm = this.form.value;
    this.partner.name = partnerForm.name;
    this.partner.description = partnerForm.description;
    this.partner.type = partnerForm.type;
    this.partner.email = partnerForm.email;
    this.partner.email2 = partnerForm.email2;
    this.partner.email3 = partnerForm.email3;
    this.partner.phone = partnerForm.phone;
    this.partner.phone2 = partnerForm.phone2;
    this.partner.phone3 = partnerForm.phone3;
    this.partner.city = partnerForm.city;
    this.partner.country = partnerForm.country;
    this.partner.address = partnerForm.address;
    this.partner.taxIdentificationNumber = partnerForm.taxIdentificationNumber;
    this.partner.registrationNumber = partnerForm.registrationNumber;
    this.partner.bankAccountNumber = partnerForm.bankAccountNumber;
    this.partner.logoUrl = partnerForm.logoUrl;

    this._partnerService.createPartner(this.partner).subscribe((partnerID) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Partner Created Successfully",4000);
      this._router.navigateByUrl('/dashboard/partners/add-partner-logo/'+partnerID);
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    });
  }

}

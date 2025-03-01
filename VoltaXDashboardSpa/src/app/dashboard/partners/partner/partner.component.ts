import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { PartnerService } from 'src/_services/partner.service';
import { environment } from 'src/environments/environment';

enum PartnerTabsEnum {
  InformationsTab = "InformationsTab",
  ChargingStationsTab = "ChargingStationsTab"
}

@Component({
  selector: 'app-partner',
  templateUrl: './partner.component.html',
  styleUrls: ['./partner.component.sass']
})
export class PartnerComponent implements OnInit {

    tabsEnum : PartnerTabsEnum = PartnerTabsEnum.InformationsTab;
    staticUrl : string = environment.apiStaticFilesUrl;
  
    partnerID : number = 0;
    partnerLoaded : boolean = false;
    partnerTypeValues : { [key: number]: string; } = {};
    updatePartnerObservable = (id : number, model : any) => this._partnerService.edit(id, model);

    editingImage: boolean = false;
    imageUploading: boolean = false;
    displayedLogo: string | null = null;
    selectedFile: File | null = null;

    
    

    partner: any = {};
  
  
    // Form group
    partnerForm : FormGroup;
  
    constructor(
      private _partnerService : PartnerService,
      private _route: ActivatedRoute,
      private _enumService : EnumMappingService,
      private _modalService: ActionModalService
    ) {
      this.partnerForm = new FormGroup({
        serialNumber : new FormControl(''),
        make : new FormControl(''),
        status : new FormControl(''),
        category : new FormControl(''),
        comment : new FormControl(''),
        partnerCategory : new FormControl(''),
      })
     }
  
    ngOnInit() {
      var idParam = this._route.snapshot.paramMap.get('id')
      if (idParam != null) {
        var id = parseInt(idParam);
        this.getPartnerByID(id);
      }
    }
  
  
    getPartnerByID(id : number){
      this.partnerID = id;
      this._partnerService.getPartnerByID(id).subscribe((cs) => {
        this.partner = cs;
        this.partnerLoaded = true;
      })
    }
  
    changeTab(tab : any){
      this.tabsEnum = tab;
    }

    handleUpload(event: Event): void {
      const input = event.target as HTMLInputElement;
      if (input.files && input.files.length > 0) {
        this.selectedFile = input.files[0];
  
        // Preview the image
        const reader = new FileReader();
        reader.onload = () => {
          this.displayedLogo = reader.result as string;
        };
        reader.readAsDataURL(this.selectedFile);
      }
    }
  
    clearImage(): void {
      this.displayedLogo = null;
      this.selectedFile = null;
    }
  
    changeLogo(): void {
      if (!this.selectedFile) return;

      console.log("this is the partner ID : " , this.partnerID);
  
      this.imageUploading = true;
      const formData = new FormData();
      formData.append('imageFile', this.selectedFile);

      this._partnerService.uploadPartnerLogo(formData, this.partner.id).subscribe((data) => {
        this.imageUploading = false;
        this.editingImage = false;
        this.getPartnerByID(this.partnerID);
        this._modalService.popup(ActionModalStatusEnum.Success,"Success !", "Image Uploaded Successfully ! ",4000);
      },(error) => {
        this.imageUploading = false;
        this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something Went wrong please try again later", 4000);
      })
    }

}

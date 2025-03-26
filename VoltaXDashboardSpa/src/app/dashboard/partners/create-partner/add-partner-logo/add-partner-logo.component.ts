import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { PartnerService } from 'src/_services/partner.service';

@Component({
  selector: 'app-add-partner-logo',
  templateUrl: './add-partner-logo.component.html',
  styleUrls: ['./add-partner-logo.component.sass']
})
export class AddPartnerLogoComponent implements OnInit {

  partnerID : number = 0;
  imageUploading: boolean = false;
  displayedLogo: string | null = null;
  selectedFile: File | null = null;
  timestamp : any = new Date().getTime();
  
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

    this.imageUploading = true;
    const formData = new FormData();
    formData.append('imageFile', this.selectedFile);

    this._partnerService.uploadPartnerLogo(formData, this.partnerID).subscribe((data) => {
      this.imageUploading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !", "Partner Logo Added Successfully ! ",4000);
      this._router.navigateByUrl("/dashboard/partners");
    },(error) => {
      this.imageUploading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something Went wrong please try again later", 4000);
    })
  }

  constructor(
    private _route : ActivatedRoute,
    private _partnerService : PartnerService,
    private _modalService : ActionModalService,
    private _router : Router
  ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      this.partnerID = parseInt(idParam);
    }
  }

  skip(){
    this._router.navigateByUrl("/dashboard/partners");
  }

}

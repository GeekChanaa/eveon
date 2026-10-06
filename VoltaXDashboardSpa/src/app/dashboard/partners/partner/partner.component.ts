import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { PageState } from 'src/_models/_enums/page-state.enum';
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

  tabsEnum: PartnerTabsEnum = PartnerTabsEnum.InformationsTab;
  readonly tabs = [
    { id: PartnerTabsEnum.InformationsTab, label: 'Information', description: 'Profile, contact & business details', icon: 'M12 11v6M12 7v1M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0' },
    { id: PartnerTabsEnum.ChargingStationsTab, label: 'Charging stations', description: 'Stations managed by this partner', icon: 'M8 3v5m8-5v5M6 8h12v4a6 6 0 0 1-12 0V8Zm6 10v4' }
  ];
  staticUrl: string = environment.apiStaticFilesUrl;

  PageState = PageState;
  state: PageState = PageState.Loading;

  partnerID: number = 0;
  partnerLoaded: boolean = false;
  partnerTypeValues: { [key: number]: string; } = {};
  updatePartnerObservable = (id: number, model: any) => this._partnerService.edit(id, model);

  editingImage: boolean = false;
  imageUploading: boolean = false;
  displayedLogo: string | null = null;
  selectedFile: File | null = null;
  timestamp: any = new Date().getTime();

  


  partner: any = {};


  // Form group
  partnerForm: FormGroup;

  constructor(
    private _partnerService: PartnerService,
    private _route: ActivatedRoute,
    private _enumService: EnumMappingService,
    private _modalService: ActionModalService,
    private _fb : FormBuilder
  ) {
    this.partnerForm = new FormGroup({
      serialNumber: new FormControl(''),
      make: new FormControl(''),
      status: new FormControl(''),
      category: new FormControl(''),
      comment: new FormControl(''),
      partnerCategory: new FormControl(''),
    })
  }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getPartnerByID(id);
    }
  }


  getPartnerByID(id: number) {
    this.state = PageState.Loading;
    this.partnerID = id;
    this._partnerService.getPartnerByID(id).subscribe((cs) => {
      this.state = PageState.Success;
      this.partner = cs;
      this.partnerLoaded = true;
      console.log("this is the partner");
      console.log(this.partner);
      this.partnerForm = this._fb.group({
        email: [this.partner.email, [Validators.required,Validators.email]],
        email2: [this.partner.email2, [Validators.required,Validators.email]],
        email3: [this.partner.email3, [Validators.required,Validators.email]],
        phone: [this.partner.phone, [Validators.required,Validators.pattern('^((\\+91-?)|0)?[0-9]{10}$') ]],
        phone2: [this.partner.phone2, [Validators.required,Validators.pattern('^((\\+91-?)|0)?[0-9]{10}$') ]],
        phone3: [this.partner.phone3, [Validators.required,Validators.pattern('^((\\+91-?)|0)?[0-9]{10}$') ]]
      });
    })
  }

  getControl(name: string): FormControl {
    return this.partnerForm.get(name) as FormControl;
  }

  changeTab(tab: any) {
    this.tabsEnum = tab;
  }

  onTabKey(event: KeyboardEvent, index: number): void {
    let next = index;
    if (event.key === 'ArrowRight') next = (index + 1) % this.tabs.length;
    else if (event.key === 'ArrowLeft') next = (index + this.tabs.length - 1) % this.tabs.length;
    else if (event.key === 'Home') next = 0;
    else if (event.key === 'End') next = this.tabs.length - 1;
    else return;
    event.preventDefault();
    this.changeTab(this.tabs[next].id);
    const tablist = (event.currentTarget as HTMLElement).parentElement;
    (tablist?.querySelectorAll('button')[next] as HTMLButtonElement)?.focus();
  }


  startEditing() {
    this.editingImage = true;
  }

  // Handle upload drag & drop or file select
  handleUpload(event: any) {
    let file: File;

    if (event.dataTransfer) {
      file = event.dataTransfer.files[0];
    } else if (event.target.files) {
      file = event.target.files[0];
    } else return;

    // Validate type
    if (!file.type.match(/image\/(jpeg|png|jpg)/)) {
      this._modalService.popup(ActionModalStatusEnum.Error, 'Invalid File', 'Only JPG/PNG images are allowed', 4000);
      return;
    }

    // Validate size
    if (file.size > 3 * 1024 * 1024) {
      this._modalService.popup(ActionModalStatusEnum.Error, 'File too large', 'Maximum file size is 3MB', 4000);
      return;
    }

    this.selectedFile = file;

    // Display preview
    const reader = new FileReader();
    reader.onload = e => this.displayedLogo = reader.result as string;
    reader.readAsDataURL(file);
  }

  isDragging = false;

  onDragOver(event: DragEvent) {
  event.preventDefault();
  event.stopPropagation();
  this.isDragging = true;
}

onDragLeave(event: DragEvent) {
  event.preventDefault();
  event.stopPropagation();
  // Only set isDragging to false if we're leaving the dropzone entirely
  const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
  const x = event.clientX;
  const y = event.clientY;
  
  if (x < rect.left || x >= rect.right || y < rect.top || y >= rect.bottom) {
    this.isDragging = false;
  }
}

onDrop(event: DragEvent) {
  event.preventDefault();
  event.stopPropagation();
  this.isDragging = false;

  if (event.dataTransfer && event.dataTransfer.files.length > 0) {
    const file = event.dataTransfer.files[0];
    
    // Create a fake event object that matches what handleUpload expects
    const fakeEvent = {
      target: { files: [file] },
      dataTransfer: { files: [file] }
    };
    
    this.handleUpload(fakeEvent);
  }
}

// Also update clearImage to reset the dragging state:
clearImage(): void {
  this.displayedLogo = null;
  this.selectedFile = null;
  this.isDragging = false;
}


  changeLogo(): void {
    if (!this.selectedFile) return;

    this.imageUploading = true;
    const formData = new FormData();
    formData.append('imageFile', this.selectedFile);

    this._partnerService.uploadPartnerLogo(formData, this.partner.id).subscribe(
      () => {
        this.imageUploading = false;
        this.editingImage = false;
        this.getPartnerByID(this.partnerID);
        this._modalService.popup(ActionModalStatusEnum.Success, "Success !", "Image Uploaded Successfully !", 4000);
      },
      () => {
        this.imageUploading = false;
        this._modalService.popup(ActionModalStatusEnum.Error, "Error !", "Something went wrong. Please try again later", 4000);
      }
    );
  }

}

import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ChargingStationImageService } from 'src/_services/charging-station-image.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-charging-station-images',
  templateUrl: './charging-station-images.component.html',
  styleUrls: ['./charging-station-images.component.sass']
})
export class ChargingStationImagesComponent implements OnInit, OnDestroy {

  @Input() chargingStationID: number = 0;
  images: any[] = [];
  loading = true;
  loadError = false;
  dragging = false;
  rootPathUrl: string = environment.apiStaticFilesUrl;
  addingImages: boolean = false;

  chargingStationImages: File[] = [];
  displayedImages: string[] = [];
  fileErrors: string[] = [];

  imagesUploading: boolean = false;

  constructor(
    private _chargingStationImageService: ChargingStationImageService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
    this.getImages();
  }

  getImages() {
    this.loading = true;
    this.loadError = false;
    this._chargingStationImageService.getChargingStationImages(this.chargingStationID).subscribe({
      next: data => { this.images = data; this.loading = false; },
      error: () => { this.loadError = true; this.loading = false; }
    });
  }

  deleteImage(id: number) {
    this._chargingStationImageService.deleteById(id).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Succcess !", "Image Removed Successfully", 4000);
      this.getImages();
    }, (error) => {
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !", "Something went wrong please try again later", 4000);
      this.getImages();
    })
  }


  handleUpload(event: any): void {
    if (this.imagesUploading) return;
    this.fileErrors = [];
    if (event.target.files && event.target.files[0]) {
      for (var i = 0; i < event.target.files.length; i++) {
        const file = event.target.files[i];
        // Validate file type
        if (!file.type.startsWith('image/')) {
          this.fileErrors.push('Only image files are allowed.');
          continue;
        }

        // Validate file size
        const maxSizeInMB = 2;
        const maxSizeInBytes = maxSizeInMB * 1024 * 1024;
        if (file.size > maxSizeInBytes) {
          this.fileErrors.push('File size must be less than 2MB.');
          continue;
        }
        this.displayedImages.push(URL.createObjectURL(event.target.files[i]))
        this.chargingStationImages.push(event.target.files[i]);
      }
    }
  }

  clearImage(i: number, event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    if (this.imagesUploading) return;
    URL.revokeObjectURL(this.displayedImages[i]);
    this.chargingStationImages.splice(i, 1);
    this.displayedImages.splice(i, 1);
  }

  uploadImages() {
    if (this.imagesUploading || !this.chargingStationImages.length) return;
    this.imagesUploading = true;
    let formData = new FormData();

    if (this.chargingStationImages) {
      this.chargingStationImages.forEach((image: File, index: number) => {
        formData.append(`chargingStationImages[${index}]`, image, image.name);
      });
    }

    this._chargingStationImageService.uploadChargingStationImages(formData, this.chargingStationID).subscribe((data) => {
      this.imagesUploading = false;
      this._modalService.popup(ActionModalStatusEnum.Success, "Success!", "Images Uploaded Successfully", 4000);
      
      // Reset state and close adding images view
      this.chargingStationImages = [];
      this.displayedImages.forEach(url => URL.revokeObjectURL(url));
      this.displayedImages = [];
      this.addingImages = false;
      
      // Refresh the images list
      this.getImages();
    }, (error) => {
      this.imagesUploading = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error!", "Something went wrong please try again later", 4000);
    })
  }

  trackByImageId(index: number, image: any): any {
    return image.id || index;
  }

  trackByIndex(index: number, item: any): number {
    return index;
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragging = !this.imagesUploading;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragging = false;
    if (this.imagesUploading) return;
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {

      // Process the dropped files
      this.fileErrors = [];
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        
        // Validate file type
        if (!file.type.startsWith('image/')) {
          this.fileErrors.push('Only image files are allowed.');
          continue;
        }

        // Validate file size
        const maxSizeInMB = 2;
        const maxSizeInBytes = maxSizeInMB * 1024 * 1024;
        if (file.size > maxSizeInBytes) {
          this.fileErrors.push('File size must be less than 2MB.');
          continue;
        }
        
        this.displayedImages.push(URL.createObjectURL(file));
        this.chargingStationImages.push(file);
      }
    }
  }

  cancelAddingImages(): void {
    if (this.imagesUploading) return;
    this.addingImages = false;
    this.displayedImages.forEach(url => URL.revokeObjectURL(url));
    // Clear any selected images and errors
    this.chargingStationImages = [];
    this.displayedImages = [];
    this.fileErrors = [];
  }

  ngOnDestroy(): void {
    this.displayedImages.forEach(url => URL.revokeObjectURL(url));
  }

}

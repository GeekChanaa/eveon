import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { ChargingStationCategoryEnum } from 'src/_models/_enums/charging-station-category';
import { PartnerService } from 'src/_services/partner.service';

@Component({
  selector: 'app-create-charging-station-images',
  templateUrl: './create-charging-station-images.component.html',
  styleUrls: ['./create-charging-station-images.component.sass']
})
export class CreateChargingStationImagesComponent implements OnInit {

  @Input() form: FormGroup = new FormGroup({});
  @Output() nextStep: EventEmitter<any> = new EventEmitter();
  @Output() previousStepEvent: EventEmitter<void> = new EventEmitter();
  
  chargingStationImages: File[] = [];
  displayedImages: string[] = [];
  fileErrors: string[] = [];
  partners: any[] = [];
  isDragging: boolean = false;
  
  constructor() { }
  
  ngOnInit() {
  }
  
  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }
  
  saveInformations() {
    this.nextStep.emit(this.chargingStationImages);
  }
  
  previousStep = () => this.previousStepEvent.emit();
  
  handleUpload(event: any): void {
    this.fileErrors = [];
    if (event.target.files && event.target.files.length > 0) {
      this.processFiles(event.target.files);
    }
  }
  
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }
  
  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }
  
  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
    
    if (event.dataTransfer && event.dataTransfer.files.length > 0) {
      this.processFiles(event.dataTransfer.files);
    }
  }
  
  processFiles(files: FileList): void {
    this.fileErrors = [];
    
    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      
      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.fileErrors.push(`"${file.name}" is not an image file.`);
        continue;
      }
      
      // Validate file size
      const maxSizeInMB = 2;
      const maxSizeInBytes = maxSizeInMB * 1024 * 1024;
      if (file.size > maxSizeInBytes) {
        this.fileErrors.push(`"${file.name}" exceeds the 2MB size limit.`);
        continue;
      }
      
      // Add valid file
      this.displayedImages.push(URL.createObjectURL(file));
      this.chargingStationImages.push(file);
    }
  }
  
  clearImage(i: number): void {
    // Revoke object URL to prevent memory leaks
    URL.revokeObjectURL(this.displayedImages[i]);
    this.chargingStationImages.splice(i, 1);
    this.displayedImages.splice(i, 1);
  }
  
  ngOnDestroy(): void {
    // Clean up object URLs to prevent memory leaks
    this.displayedImages.forEach(url => URL.revokeObjectURL(url));
  }

}

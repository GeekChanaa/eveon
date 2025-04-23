import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'app-create-charging-station-preview',
  templateUrl: './create-charging-station-preview.component.html',
  styleUrls: ['./create-charging-station-preview.component.sass']
})
export class CreateChargingStationPreviewComponent implements OnInit {
  @Input() chargingStationInformationsForm!: FormGroup;
  @Input() chargingStationAddressForm!: FormGroup;
  @Input() chargingStationImages: File[] = [];
  @Input() chargePoints: any[] = [];

  @Output() submitEvent: EventEmitter<any> = new EventEmitter();
  @Output() previousStepEvent: EventEmitter<void> = new EventEmitter();

  previousStep(){
    this.previousStepEvent.emit();
  }

  submit(){
    this.submitEvent.emit();
  }
  
  
  selectedImageIndex: number = 0;
  
  constructor() { }

  ngOnInit(): void {
  }
  
  getImageUrl(index: number): string {
    if (index >= 0 && index < this.chargingStationImages.length) {
      return URL.createObjectURL(this.chargingStationImages[index]);
    }
    return '';
  }
  
  getStatusClass(): string {
    const status = this.chargingStationInformationsForm.get('status')?.value;
    
    switch (status) {
      case 'Available':
        return 'status-available';
      case 'Unavailable':
        return 'status-unavailable';
      case 'Maintenance':
        return 'status-maintenance';
      default:
        return '';
    }
  }
  
  formatParkingType(parkingType: string): string {
    if (!parkingType) return '';
    
    // Convert camelCase to spaces
    return parkingType.replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase());
  }
  
  hasAmenities(): boolean {
    return (
      this.chargingStationInformationsForm.get('wifi')?.value ||
      this.chargingStationInformationsForm.get('parking')?.value ||
      this.chargingStationInformationsForm.get('restaurants')?.value ||
      this.chargingStationInformationsForm.get('washroom')?.value ||
      this.chargingStationInformationsForm.get('sittingArea')?.value
    );
  }
  
  openImageGallery(): void {
    // This would open a modal or fullscreen gallery
    console.log('Opening full image gallery');
    // Implementation would depend on your modal/dialog system
  }
  
  ngOnDestroy(): void {
    // Clean up object URLs to prevent memory leaks
    for (let i = 0; i < this.chargingStationImages.length; i++) {
      URL.revokeObjectURL(this.getImageUrl(i));
    }
  }
}

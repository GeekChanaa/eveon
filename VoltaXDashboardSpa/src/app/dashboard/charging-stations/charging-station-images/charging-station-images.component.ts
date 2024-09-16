import { Component, Input, OnInit } from '@angular/core';
import { ChargingStationImageService } from 'src/_services/charging-station-image.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-charging-station-images',
  templateUrl: './charging-station-images.component.html',
  styleUrls: ['./charging-station-images.component.sass']
})
export class ChargingStationImagesComponent implements OnInit {

  @Input() chargingStationID : number = 0;
  images : any[] = [];
  rootPathUrl : string = environment.apiStaticFilesUrl;
  addingImages : boolean = false;

  chargingStationImages : File[] = [];
  displayedImages : string[] = [];
  fileErrors : string[] = [];


  constructor(
    private _chargingStationImageService : ChargingStationImageService
  ) { }

  ngOnInit() {
    this.getImages();
  }

  getImages(){
    this._chargingStationImageService.getChargingStationImages(this.chargingStationID).subscribe((data) => {
      console.log("this is the images");
      console.log(data);
      this.images = data;
    })
  }

  
  handleUpload(event: any): void {
    this.fileErrors = [];
    if (event.target.files && event.target.files[0]) {
      for(var i=0; i < event.target.files.length ; i++){
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

  clearImage(i : number): void {
    this.chargingStationImages.splice(i,1);
    this.displayedImages.splice(i,1);
  }

}

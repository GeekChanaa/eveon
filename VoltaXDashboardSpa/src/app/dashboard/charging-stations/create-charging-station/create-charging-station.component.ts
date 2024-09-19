import { AfterViewInit, Component, OnInit } from '@angular/core';
import { FormArray, FormGroup, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { Action } from 'rxjs/internal/scheduler/Action';
import { ChargePointCreateDto } from 'src/_models/_dtos/charge-point-create-dto';
import { ChargingStationCreateDto } from 'src/_models/_dtos/charging-station-create-dto';
import { CityNameDto } from 'src/_models/_dtos/city-name-dto';
import { ConnectorCreateDto } from 'src/_models/_dtos/connector-create-dto';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { CityService } from 'src/_services/city.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

declare var $: any;

@Component({
  selector: 'app-create-charging-station',
  templateUrl: './create-charging-station.component.html',
  styleUrls: ['./create-charging-station.component.sass']
})
export class CreateChargingStationComponent implements OnInit, AfterViewInit  {

  form: FormGroup;
  opacity: number = 0;
  activeDiv = 1;

  latitude : number = 0;
  longitude : number = 0;

  chargingStationCategories : any = {};

  userLatitude : number = 0;
  userLongitude : number = 0;

  chargingStationImages : File[] = [];
  displayedImages : string[] = [];
  fileErrors : string[] = [];

  chargePointIDTouched : boolean = false;
  chargePointSerialNumberTouched : boolean = false;
  checkingChagePointID : boolean = false;
  checkingChargePointSerialNumber : boolean = false;
  chargePointTimeout: any = {};
  chargePointSerialNumberTimeout: any = {};
  chargePointExist : boolean = false;
  chargePointSerialNumberExist : boolean = false;

  ngAfterViewInit() {
  }

  showNextDiv() {
    this.activeDiv = this.activeDiv === 3 ? 1 : this.activeDiv + 1;
  }

  showPreviousDiv() {
    if(this.activeDiv == 1) return; 
    this.activeDiv = this.activeDiv - 1;
  }

  // lists of  cities
  cities: CityNameDto[] = [];


  constructor(
    private _cityService: CityService,
    private _chargingStationService: ChargingStationService,
    private _enumService : EnumMappingService,
    private _modalService:  ActionModalService,
    private _router : Router,
    private _authService:  AuthService,
    private _chargePointService: ChargePointService
  ) {
    this.form = new FormGroup({
      chargingStationName: new FormControl(''),
      chargingStationNetwork: new FormControl('0'),
      chargingStationCategory: new FormControl('0'),
      chargingStationChargerQuantity: new FormControl('1'),
      chargingStationAddress: new FormControl(''),
      chargingStationCity: new FormControl(''),
      chargingStationZipCode: new FormControl(''),
      chargingStationParkingType: new FormControl('ParallelParking'),
      chargingStationStatus: new FormControl('available'),
      chargingStationAmenities: new FormGroup({
        wifi: new FormControl(false),
        parking: new FormControl(false),
        restaurants: new FormControl(false),
        washroom: new FormControl(false),
        sittingArea: new FormControl(false)
      }),
      chargePoints: new FormArray([
        new FormGroup({
          chargePointSerialNumber: new FormControl(''),
          chargePointStatus: new FormControl('Available'),
          chargePointCategory: new FormControl('TheTower'),
          chargePointID: new FormControl(''),
          chargePointName: new FormControl(''),
          chargePointConnectors: new FormArray([
            new FormGroup({
              chargePointConnectorSpeed: new FormControl('7.3'),
              chargePointConnectorPricePerKWh : new FormControl(""),
              chargePointConnectorPricePerMinute : new FormControl(""),
              chargePointConnectorPricePerHour : new FormControl("")
            })
          ])
        })
      ])
    });
  }

  get chargePoints() {
    return this.form.get('chargePoints') as FormArray;
  }

  getChargePointConnectors(i: number) {
    return ((this.form.get('chargePoints') as FormArray).at(i).get('chargePointConnectors') as FormArray);
  }

  addChargePoint() {
    const chargePoints = this.form.get('chargePoints') as FormArray;

    if (chargePoints.length < 5) {
      chargePoints.push(new FormGroup({
        chargePointSerialNumber: new FormControl(''),
        chargePointStatus: new FormControl('Available'),
        chargePointCategory: new FormControl('TheTower'),
        chargePointName: new FormControl(''),
        chargePointID: new FormControl(''),
        chargePointConnectors: new FormArray([
          new FormGroup({
            chargePointConnectorSpeed: new FormControl('7.3'),
            chargePointConnectorPricePerKWh : new FormControl(""),
            chargePointConnectorPricePerMinute : new FormControl(""),
            chargePointConnectorPricePerHour : new FormControl("")
          })
        ])
      }));
    } else {
      this._modalService.popup(ActionModalStatusEnum.Warning,"Attention !","Maximum 5 charge points are allowed",4000);
    }
  }


  addChargePointConnector(i: number) {
    ((this.form.get('chargePoints') as FormArray).at(i).get('chargePointConnectors') as FormArray).push(new FormGroup({
      chargePointConnectorSpeed: new FormControl('7.3'),
      chargePointConnectorPricePerKWh : new FormControl(""),
      chargePointConnectorPricePerMinute : new FormControl(""),
      chargePointConnectorPricePerHour : new FormControl("")
    }));
  }

  ngOnInit() {
    this.getAllCities();
    this.chargingStationCategories = Object.values(this._enumService.getEnumMapping("ChargingStationCategoryEnum"));
    this._authService.getUserInformations().then((data : any) => {
      this.userLatitude = data.latitude;
      this.userLongitude = data.longitude;
    },(error) => {
      this.userLatitude = 35.7595;
      this.userLongitude = -5.8340;
    })
  }

  // Getting cities by state
  getAllCities() {
    this._cityService.getAllMoroccoCityNames().subscribe((data) => {
      this.cities = data;
    })
  }

  onSubmit() {
    const formValues = this.form.value;
    const chargingStationFormData = this.prepareFormData(formValues, this.userLatitude.toString(), this.userLongitude.toString(), this.chargingStationImages);
    this._chargingStationService.createChargingStation(chargingStationFormData).subscribe((createdStation) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Charging Station Created Successfully",4000);
      this._router.navigateByUrl('/dashboard/charging-stations');
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    });
  }


  showSelect() {
    this.opacity = 1;
  }

  hideSelect() {
    this.opacity = 0;
  }


  // Getting City Control
  get cityControl(): FormControl {
    const control = this.form.get('chargingStationCity');
    if (!control) {
      throw new Error('City control not found');
    }
    return control as FormControl;
  }

  // Updating city Control
  updateCity(value: any) {
    this.cityControl.setValue(value.name);
  }


  removeChargePoint(index: number) {
    const chargePoints = this.form.get('chargePoints') as FormArray;
    chargePoints.removeAt(index);
  }
  removeChargePointConnector(chargePointIndex: number, connectorIndex: number) {
    const connectors = this.getChargePointConnectors(chargePointIndex);
    connectors.removeAt(connectorIndex);
  }

  getFormControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  getChargingStationCategoriesKeys(){
    return Object.keys(this.chargingStationCategories);
  }

  selectMap(location : any){
    this.latitude = location.latitude;
    this.longitude = location.longitude;
  }
  prepareFormData(formValues: any, latitude: string, longitude: string, chargingStationImages : File[]): FormData {
    let formData = new FormData();
  
    formData.append('Address', formValues.chargingStationAddress);
    formData.append('Network', formValues.chargingStationNetwork.toString());
    formData.append('Category', formValues.chargingStationCategory.toString());
    formData.append('ChargerQuantity', formValues.chargingStationChargerQuantity.toString());
    formData.append('City', formValues.chargingStationCity);
    formData.append('ParkingType', formValues.chargingStationParkingType);
    formData.append('Status', formValues.chargingStationStatus.toString());
    formData.append('WifiAmenity', formValues.chargingStationAmenities.wifi.toString());
    formData.append('ParkingAmenity', formValues.chargingStationAmenities.parking.toString());
    formData.append('RestaurantsAmenity', formValues.chargingStationAmenities.restaurants.toString());
    formData.append('WashroomAmenity', formValues.chargingStationAmenities.washroom.toString());
    formData.append('SittingAreaAmenity', formValues.chargingStationAmenities.sittingArea.toString());
    formData.append('Latitude', latitude.toString());
    formData.append('Longitude', longitude.toString());
    if (this.chargingStationImages) {
      this.chargingStationImages.forEach((image : File, index : number) => {
        formData.append(`chargingStationImages[${index}]`, image, image.name);
      });
    }
  
    formValues.chargePoints.forEach((cp: any, chargePointIndex: number) => {
      formData.append(`chargePoints[${chargePointIndex}].name`, cp.chargePointName);
      formData.append(`chargePoints[${chargePointIndex}].chargePointId`, cp.chargePointID.toString());
      formData.append(`chargePoints[${chargePointIndex}].serialNumber`, cp.chargePointSerialNumber);
      formData.append(`chargePoints[${chargePointIndex}].make`, 'VoltaX');
      formData.append(`chargePoints[${chargePointIndex}].status`, cp.chargePointStatus);
      formData.append(`chargePoints[${chargePointIndex}].comment`, '');  // Empty as per original logic
      formData.append(`chargePoints[${chargePointIndex}].username`, '');
      formData.append(`chargePoints[${chargePointIndex}].password`, '');
      formData.append(`chargePoints[${chargePointIndex}].clientCertThumb`, '');
      formData.append(`chargePoints[${chargePointIndex}].category`, cp.chargePointCategory);
  
      cp.chargePointConnectors.forEach((connector: any, connectorIndex: number) => {
        formData.append(`chargePoints[${chargePointIndex}].connectors[${connectorIndex}].connectorType`, 'cType2');
        formData.append(`chargePoints[${chargePointIndex}].connectors[${connectorIndex}].speed`, connector.chargePointConnectorSpeed);
        formData.append(`chargePoints[${chargePointIndex}].connectors[${connectorIndex}].pricePerKWh`, connector.chargePointConnectorPricePerKWh.toString());
        formData.append(`chargePoints[${chargePointIndex}].connectors[${connectorIndex}].pricePerMinute`, connector.chargePointConnectorPricePerMinute.toString());
        formData.append(`chargePoints[${chargePointIndex}].connectors[${connectorIndex}].pricePerHour`, connector.chargePointConnectorPricePerHour.toString());
      });
    });
  
    return formData;
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

  isChargePointIDUnique(chargePointID: string): void {
    this.chargePointIDTouched = true;
    this.checkingChagePointID = true;
    clearTimeout(this.chargePointTimeout);
    this.chargePointTimeout = setTimeout(() => {
      this._chargePointService.isChargePointIDUnique(chargePointID).subscribe(
        (data) => {
          this.chargePointExist = data;
          this.checkingChagePointID = false;
        },
        (error) => {
          clearTimeout(this.chargePointTimeout);
        }
      );
    }, 800);
  }


  isChargePointSerialNumberUnique(chargePointSerialNumber: string): void {
    this.chargePointSerialNumberTouched = true;
    this.checkingChargePointSerialNumber = true;
    clearTimeout(this.chargePointSerialNumberTimeout);
    this.chargePointSerialNumberTimeout = setTimeout(() => {
      this._chargePointService.isChargePointSerialNumberUnique(chargePointSerialNumber).subscribe(
        (data) => {
          this.chargePointSerialNumberExist = data;
          this.checkingChargePointSerialNumber = false;
        },
        (error) => {
          clearTimeout(this.chargePointSerialNumberTimeout);
        }
      );
    }, 800);
  }
  
}

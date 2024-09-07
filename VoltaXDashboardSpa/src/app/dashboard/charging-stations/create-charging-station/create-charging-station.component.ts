import { AfterViewInit, Component, OnInit } from '@angular/core';
import { FormArray, FormGroup, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { ChargePointCreateDto } from 'src/_models/_dtos/charge-point-create-dto';
import { ChargingStationCreateDto } from 'src/_models/_dtos/charging-station-create-dto';
import { CityNameDto } from 'src/_models/_dtos/city-name-dto';
import { ConnectorCreateDto } from 'src/_models/_dtos/connector-create-dto';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
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
    private _router : Router
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
    this.chargingStationCategories = Object.values(this._enumService.getEnumMapping("ChargingStationCategoryEnum"))
  }

  // Getting cities by state
  getAllCities() {
    this._cityService.getAllMoroccoCityNames().subscribe((data) => {
      this.cities = data;
    })
  }

  onSubmit() {
    // Extract form values
    const formValues = this.form.value;

    console.log("this is the form value");
    console.log(formValues);

    // Create ChargingStation object
    const chargingStation: ChargingStationCreateDto = {
      Address: formValues.chargingStationAddress,
      Network: formValues.chargingStationNetwork,
      Category: formValues.chargingStationCategory,
      ChargerQuantity: formValues.chargingStationChargerQuantity,
      City: formValues.chargingStationCity,
      ParkingType: formValues.chargingStationParkingType,
      Status: formValues.chargingStationStatus,
      WifiAmenity: formValues.chargingStationAmenities.wifi,
      ParkingAmenity: formValues.chargingStationAmenities.parking,
      RestaurantsAmenity: formValues.chargingStationAmenities.restaurants,
      WashroomAmenity: formValues.chargingStationAmenities.washroom,
      SittingAreaAmenity: formValues.chargingStationAmenities.sittingArea,
      Latitude: this.latitude.toString(),
      Longitude: this.longitude.toString(),
      chargePoints : []
    };
    formValues.chargePoints.forEach((cp : any) => {
      let chargePoint: ChargePointCreateDto = {
        name: cp.chargePointName,
        chargePointId: cp.chargePointID,
        serialNumber: cp.chargePointSerialNumber,
        make: "VoltaX",
        status: cp.chargePointStatus,
        comment: '',
        username: '',
        password: '',
        clientCertThumb: '',
        category: cp.chargePointCategory,
        connectors : []
      };

      cp.chargePointConnectors.forEach((connector: any) => {
        const connectorObj: ConnectorCreateDto = {
          connectorType: "cType2",
          speed: connector.chargePointConnectorSpeed,
          pricePerKWh: connector.chargePointConnectorPricePerHour,
          pricePerMinute: connector.chargePointConnectorPricePerMinute,
          pricePerHour: connector.chargePointConnectorPricePerHour
        };
        chargePoint.connectors.push(connectorObj)
        
      });

      chargingStation.chargePoints.push(chargePoint);
    })

    // Create the station first because the chargePoints depend on its ID
    this._chargingStationService.createChargingStation(chargingStation).subscribe((createdStation) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Charging Station Created Successfully",4000);
      this._router.navigateByUrl('/dashboard/charging-stations');
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

}

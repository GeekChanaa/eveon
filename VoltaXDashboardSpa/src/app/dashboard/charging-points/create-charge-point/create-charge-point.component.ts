import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, FormArray, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

@Component({
  selector: 'app-create-charge-point',
  templateUrl: './create-charge-point.component.html',
  styleUrls: ['./create-charge-point.component.sass']
})
export class CreateChargePointComponent implements OnInit {

  form: FormGroup;
  opacity: number = 0;
  activeDiv = 1;
  isLoading : boolean = false;

  chargingStationID : number = 0;

  latitude : number = 0;
  longitude : number = 0;
  chargingStations : any[] = [];

  chargingStationCategories : any = {};


  chargingStationImages : File[] = [];
  displayedImages : string[] = [];
  fileErrors : string[] = [];

  chargePointIDTouched : boolean = false;
  chargePointSerialNumberTouched : boolean = false;
  checkingChagePointID : boolean = false;
  checkingChargePointSerialNumber : boolean = false;

  chargingStationNameExists : boolean = false;
  checkingChargingStationName : boolean = false;

  chargePointTimeout: any = {};
  chargePointSerialNumberTimeout: any = {};
  chargingStationNameTimeout: any = {};
  chargePointExist : boolean = false;
  chargePointSerialNumberExist : boolean = false;

  ngAfterViewInit() {
  }

  constructor(
    private _chargingStationService: ChargingStationService,
    private _enumService : EnumMappingService,
    private _modalService:  ActionModalService,
    private _router : Router,
    private _authService:  AuthService,
    private _chargePointService: ChargePointService
  ) {
    this.form = new FormGroup({
      chargePointSerialNumber: new FormControl(''),
      chargePointStatus: new FormControl('Available'),
      chargePointCategory: new FormControl('TheTower'),
      chargePointID: new FormControl(''),
      chargingStationID: new FormControl(''),
      chargePointName: new FormControl(''),
      chargePointConnectors: new FormArray([
        new FormGroup({
          chargePointConnectorSpeed: new FormControl('7.3'),
          chargePointConnectorPricePerKWh : new FormControl(""),
          chargePointConnectorPricePerMinute : new FormControl(""),
          chargePointConnectorPricePerHour : new FormControl(""),
          chargePointConnectorID : new FormControl(""),
        })
      ], this.duplicateConnectorIDValidator)
    });
  }

  get chargePoints() {
    return this.form.get('chargePoints') as FormArray;
  }

  getChargePointConnectors() {
    return (this.form.get('chargePointConnectors') as FormArray);
  }

  addChargePointConnector() {
    (this.form.get('chargePointConnectors') as FormArray).push(new FormGroup({
      chargePointConnectorSpeed: new FormControl('7.3'),
      chargePointConnectorPricePerKWh : new FormControl(""),
      chargePointConnectorPricePerMinute : new FormControl(""),
      chargePointConnectorPricePerHour : new FormControl(""),
      chargePointConnectorID : new FormControl("")
    }));
  }

  ngOnInit() {
    this.chargingStationCategories = Object.values(this._enumService.getEnumMapping("ChargingStationCategoryEnum"));
    this.getChargingStationNames();
  }


  onSubmit() {
    this.isLoading = true;

    const formValues = this.form.value;
    let chargePoint : any = {};
    chargePoint.serialNumber =  formValues.chargePointSerialNumber;
    chargePoint.status =  formValues.chargePointStatus;
    chargePoint.category =  formValues.chargePointCategory;
    chargePoint.name =  formValues.chargePointName;
    chargePoint.chargePointId = formValues.chargePointID;
    chargePoint.chargingStationID = this.chargingStationID;
    chargePoint.connectors = [];
    formValues.chargePointConnectors.forEach((connector : any) => {
      let co : any = {};
      co.speed = connector.chargePointConnectorSpeed;
      co.pricePerKWh = connector.chargePointConnectorPricePerKWh;
      co.pricePerMinute = connector.chargePointConnectorPricePerMinute;
      co.pricePerHour = connector.chargePointConnectorPricePerHour;
      co.connectorID = connector.chargePointConnectorID;
      chargePoint.connectors.push(co);
    });

    this._chargePointService.create(chargePoint).subscribe((createdConnector) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Charge Point Created Successfully",4000);
      this._router.navigateByUrl('/dashboard/charging-points');
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    });
  }

  updateChargingStationID(chargingStation : any){
    this.chargingStationIDControl.setValue(chargingStation.name)
    this.chargingStationID = chargingStation.id;
  }


  showSelect() {
    this.opacity = 1;
  }

  hideSelect() {
    this.opacity = 0;
  }

  removeChargePointConnector(connectorIndex: number) {
    const connectors = this.getChargePointConnectors();
    connectors.removeAt(connectorIndex);
  }

  getFormControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  get chargingStationIDControl(): FormControl {
    const control = this.form.get('chargingStationID');
    if (!control) {
      throw new Error('City control not found');
    }
    return control as FormControl;
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

  ChargingStationNameExists(chargingStationName: string): void {
    this.chargePointIDTouched = true;
    this.checkingChargingStationName = true;
    clearTimeout(this.chargingStationNameTimeout);
    this.chargingStationNameTimeout = setTimeout(() => {
      this._chargingStationService.chargingStationExistsByName(chargingStationName).subscribe(
        (data) => {
          this.chargingStationNameExists = data;
          this.checkingChargingStationName = false;
        },
        (error) => {
          clearTimeout(this.chargingStationNameTimeout);
        }
      );
    }, 800);
  }

  getChargingStationNames(){
    this._chargingStationService.getChargingStationNames().subscribe((data) => {
      this.chargingStations = data;
    })
  }


  duplicateConnectorIDValidator(formArray: AbstractControl): ValidationErrors | null {
    const connectorIDs = formArray.value.map((connector: any) => connector.chargePointConnectorID);
    const uniqueConnectorIDs = new Set(connectorIDs);

    if (uniqueConnectorIDs.size !== connectorIDs.length) {
      return { duplicateConnectorID: true }; 
    }
    return null;
  }

}

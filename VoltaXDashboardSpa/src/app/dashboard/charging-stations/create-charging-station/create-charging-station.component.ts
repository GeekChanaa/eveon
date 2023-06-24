import { Component, OnInit } from '@angular/core';
import { FormArray, FormGroup, FormControl } from '@angular/forms';
import { ChargePointCreateDto } from 'src/_models/_dtos/charge-point-create-dto';
import { ChargingStationCreateDto } from 'src/_models/_dtos/charging-station-create-dto';
import { CityNameDto } from 'src/_models/_dtos/city-name-dto';
import { ConnectorCreateDto } from 'src/_models/_dtos/connector-create-dto';
import { ConnectorTarifCreateDto } from 'src/_models/_dtos/connector-tarif-create-dto';
import { CountryNameDto } from 'src/_models/_dtos/country-name-dto';
import { StateNameDto } from 'src/_models/_dtos/state-name-dto';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { CityService } from 'src/_services/city.service';
import { ConnectorTarifService } from 'src/_services/connector-tarif.service';
import { ConnectorService } from 'src/_services/connector.service';
import { CountryService } from 'src/_services/country.service';
import { StateService } from 'src/_services/state.service';
@Component({
  selector: 'app-create-charging-station',
  templateUrl: './create-charging-station.component.html',
  styleUrls: ['./create-charging-station.component.css']
})
export class CreateChargingStationComponent implements OnInit {

  form: FormGroup;
  opacity: number = 0;
  activeDiv = 1;

  showNextDiv() {
    this.activeDiv = this.activeDiv === 3 ? 1 : this.activeDiv + 1;
  }

  showPreviousDiv() {
    this.activeDiv = this.activeDiv - 1;
  }

  // lists of countries / states / cities
  countries: CountryNameDto[] = [];
  states: StateNameDto[] = [];
  cities: CityNameDto[] = [];


  constructor(
    private _countryService: CountryService,
    private _cityService: CityService,
    private _stateService: StateService,
    private _chargingStationService: ChargingStationService,
    private _chargePointService: ChargePointService,
    private _connectorService: ConnectorService,
    private _connectorTarifService: ConnectorTarifService
  ) {
    this.form = new FormGroup({
      chargingStationName: new FormControl(''),
      chargingStationNetwork: new FormControl(''),
      chargingStationCategory: new FormControl(''),
      chargingStationChargerQuantity: new FormControl(''),
      chargingStationAddress: new FormControl(''),
      chargingStationCountry: new FormControl(''),
      chargingStationState: new FormControl(''),
      chargingStationCity: new FormControl(''),
      chargingStationZipCode: new FormControl(''),
      chargingStationLatitude: new FormControl(''),
      chargingStationLongitude: new FormControl(''),
      chargingStationOrganisation: new FormControl(''),
      chargingStationPublish: new FormControl(''),
      chargingStationParkingType: new FormControl(''),
      chargingStationStatus: new FormControl(''),
      chargingStationAmenities: new FormGroup({
        wifi: new FormControl(false),
        parking: new FormControl(false),
        restaurants: new FormControl(false),
        washroom: new FormControl(false),
        sittingArea: new FormControl(false)
      }),
      chargePoints: new FormArray([
        new FormGroup({
          chargePointName: new FormControl(''),
          chargePointSerialNumber: new FormControl(''),
          chargePointMake: new FormControl(''),
          chargePointStatus: new FormControl(''),
          chargePointConnectors: new FormArray([
            new FormGroup({
              chargePointConnectorType: new FormControl(''),
              chargePointConnectorSpeed: new FormControl(''),
              chargePointConnectorPower: new FormControl(''),
              chargePointConnectorQuantity: new FormControl(''),
              chargePointConnectorTarifs: new FormArray([
                new FormGroup({
                  chargePointConnectorTarifUnit: new FormControl(''),
                  chargePointConnectorTarifCurrency: new FormControl(''),
                  chargePointConnectorTarifQuantity: new FormControl(''),
                })
              ])
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

  getChargePointConnectorTarifs(i: number, j: number) {
    return (((this.form.get('chargePoints') as FormArray).at(i).get('chargePointConnectors') as FormArray).at(j).get('chargePointConnectorTarifs') as FormArray);
  }

  addChargePoint() {
    const chargePoints = this.form.get('chargePoints') as FormArray;

    if (chargePoints.length < 5) {
      chargePoints.push(new FormGroup({
        chargePointName: new FormControl(''),
        chargePointSerialNumber: new FormControl(''),
        chargePointMake: new FormControl(''),
        chargePointStatus: new FormControl(''),
        chargePointConnectors: new FormArray([
          new FormGroup({
            chargePointConnectorType: new FormControl(''),
            chargePointConnectorSpeed: new FormControl(''),
            chargePointConnectorPower: new FormControl(''),
            chargePointConnectorQuantity: new FormControl(''),
            chargePointConnectorTarifs: new FormArray([
              new FormGroup({
                chargePointConnectorTarifUnit: new FormControl(''),
                chargePointConnectorTarifCurrency: new FormControl(''),
                chargePointConnectorTarifQuantity: new FormControl(''),
              })
            ])
          })
        ])
      }));
    } else {
      alert('Maximum 5 charge points are allowed');
    }
  }


  addChargePointConnector(i: number) {
    ((this.form.get('chargePoints') as FormArray).at(i).get('chargePointConnectors') as FormArray).push(new FormGroup({
      chargePointConnectorType: new FormControl(''),
      chargePointConnectorSpeed: new FormControl(''),
      chargePointConnectorPower: new FormControl(''),
      chargePointConnectorQuantity: new FormControl(''),
      chargePointConnectorTarifs: new FormArray([
        new FormGroup({
          chargePointConnectorTarifUnit: new FormControl(''),
          chargePointConnectorTarifCurrency: new FormControl(''),
          chargePointConnectorTarifQuantity: new FormControl(''),
        })
      ])
    }));
  }

  addChargePointConnectorTarif(i: number, j: number) {
    (((this.form.get('chargePoints') as FormArray).at(i).get('chargePointConnectors') as FormArray).at(j).get('chargePointConnectorTarifs') as FormArray).push(new FormGroup({
      chargePointConnectorTarifUnit: new FormControl(''),
      chargePointConnectorTarifCurrency: new FormControl(''),
      chargePointConnectorTarifQuantity: new FormControl(''),
    }));
  }

  ngOnInit() {
    this.getAllCountryNames();
  }

  // Getting all country names
  getAllCountryNames() {
    this._countryService.getAllCountryNames().subscribe((data) => {
      this.countries = data;
    })
  }

  // Getting states by country
  getAllStatesByCountry(countryID: number) {
    this._stateService.getStatesByCountryID(countryID).subscribe((data) => {
      this.states = data;
    })
  }

  // Getting cities by state
  getAllCitiesByState(stateID: number) {
    this._cityService.getCitiesByStateID(stateID).subscribe((data) => {
      this.cities = data;
    })
  }

  onSubmit() {
    // Extract form values
    const formValues = this.form.value;

    // Create ChargingStation object
    const chargingStation: ChargingStationCreateDto = {
      Name: formValues.chargingStationName,
      Address: formValues.chargingStationAddress,
      Network: formValues.chargingStationNetwork,
      Category: formValues.chargingStationCategory,
      ChargerQuantity: formValues.chargingStationChargerQuantity,
      Country: formValues.chargingStationCountry,
      State: formValues.chargingStationState,
      City: formValues.chargingStationCity,
      Latitude: formValues.chargingStationLatitude,
      Longitude: formValues.chargingStationLongitude,
      Organisation: formValues.chargingStationOrganisation,
      ParkingType: formValues.chargingStationParkingType,
      Status: formValues.chargingStationStatus,
      WifiAmenity: formValues.chargingStationAmenities.wifi,
      ParkingAmenity: formValues.chargingStationAmenities.parking,
      RestaurantsAmenity: formValues.chargingStationAmenities.restaurants,
      WashroomAmenity: formValues.chargingStationAmenities.washroom,
      SittingAreaAmenity: formValues.chargingStationAmenities.sittingArea,
    };
    console.log("charge station");

    console.log(chargingStation);

    // Create the station first because the chargePoints depend on its ID
    this._chargingStationService.create(chargingStation).subscribe((createdStation) => {
      console.log("this is the created station");
      console.log(createdStation);
      // Now create the ChargePoints
      formValues.chargePoints.forEach((cp: any) => {
        // Create ChargePoint object
        const chargePoint: ChargePointCreateDto = {
          chargingStationID: createdStation.id,
          name: cp.chargePointName,
          chargePointId: "",
          serialNumber: cp.chargePointSerialNumber,
          make: cp.chargePointMake,
          status: cp.chargePointStatus,
          comment: '',
          username: '',
          password: '',
          clientCertThumb: ''
        };

        this._chargePointService.create(chargePoint).subscribe((createdChargePoint) => {

          // Now create the Connectors
          cp.chargePointConnectors.forEach((connector: any) => {
            // Create Connector object
            const connectorObj: ConnectorCreateDto = {
              chargePointId: createdChargePoint.id,
              connectorType: connector.chargePointConnectorType,
              power: connector.chargePointConnectorPower,
              speed: connector.chargePointConnectorSpeed,
            };
            this._connectorService.create(connectorObj).subscribe((createdConnector) => {
              // Now create the ConnectorTarifs
              connector.chargePointConnectorTarifs.forEach((tarif: any) => {
                // Create ConnectorTarif object
                const connectorTarif: ConnectorTarifCreateDto = {
                  connectorID: createdConnector.id,
                  unit: tarif.chargePointConnectorTarifUnit,
                  quantity: tarif.chargePointConnectorTarifQuantity,
                  currency: tarif.chargePointConnectorTarifCurrency,
                };
                this._connectorTarifService.create(connectorTarif).subscribe();
              });
            });
          });
        });
      });
    });
  }


  showSelect() {
    console.log("this is focused")
    this.opacity = 1;
  }

  hideSelect() {
    console.log("this is focusout");
    this.opacity = 0;
  }


  // Getting City Control
  get cityControl(): FormControl {
    const control = this.form.get('chargingStationCity');
    if (!control) {
      throw new Error('Country control not found');
    }
    return control as FormControl;
  }

  // Getting Country Control
  get countryControl(): FormControl {
    const control = this.form.get('chargingStationCountry');
    if (!control) {
      throw new Error('Country control not found');
    }
    return control as FormControl;
  }

  // Getting State Control
  get stateControl(): FormControl {
    const control = this.form.get('chargingStationState');
    if (!control) {
      throw new Error('Country control not found');
    }
    return control as FormControl;
  }

  // Updating state Control
  updateState(value: any) {
    this.stateControl.setValue(value.name);
    this.getAllCitiesByState(value.id);
  }

  // Updating country Control
  updateCountry(value: any) {
    this.countryControl.setValue(value.name);
    this.getAllStatesByCountry(value.id);
  }

  // Updating city Control
  updateCity(value: any) {
    this.cityControl.setValue(value.name);
  }


  removeChargePoint(index: number) {
    const chargePoints = this.form.get('chargePoints') as FormArray;
    if (chargePoints.length > 1) {
      chargePoints.removeAt(index);
    } else {
      alert('At least one charge point should exist');
    }
  }

  removeChargePointConnector(chargePointIndex: number, connectorIndex: number) {
    const connectors = this.getChargePointConnectors(chargePointIndex);
    if (connectors.length > 1) {
      connectors.removeAt(connectorIndex);
    } else {
      alert('At least one connector should exist for each charge point');
    }
  }

  removeChargePointConnectorTarif(chargePointIndex: number, connectorIndex: number, tarifIndex: number) {
    const tarifs = this.getChargePointConnectorTarifs(chargePointIndex, connectorIndex);
    if (tarifs.length > 1) {
      tarifs.removeAt(tarifIndex);
    } else {
      alert('At least one tariff should exist for each connector');
    }
  }

  getFormControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  

}

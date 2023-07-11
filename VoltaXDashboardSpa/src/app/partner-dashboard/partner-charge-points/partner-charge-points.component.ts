import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargePoint } from 'src/_models/charge-point';
import { AuthService } from 'src/_services/auth.service';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-partner-charge-points',
  templateUrl: './partner-charge-points.component.html',
  styleUrls: ['./partner-charge-points.component.css']
})
export class PartnerChargePointsComponent implements OnInit {

  // Data
  data: any[] = [];

  // partnerID 
  partnerID : number = 0;

  // filters :
  filters: any = {};

  // item params for filtering sorting an other
  itemParams: any = {};

  // Fields
  fields: string[] = [];

  // Page params
  itemsPerPage: number = 20;
  currentPage: number = 1;

  chargePoint: ChargePoint = {
    id: 0,
    chargePointId: '',
    chargingStationID: 0,
    name: '',
    serialNumber: '',
    make: '',
    status: ChargePointStatusEnum.Available,
    comment: '',
    username: '',
    password: '',
    clientCertThumb: '',
    connectors: [],
    transactions: [],
    category: ChargePointCategoryEnum.TheTower,
  };

  // Constructor
  constructor(
    private _chargePointService: ChargePointService,
    private _router: Router,
    private _authService : AuthService
  ) {}

  ngOnInit() {
    this.partnerID = parseInt(this._authService.getAuthInformation().nameid);
    this._getItemFields();
    this.getAll();
  }

  // Getting All Products
  getAll() {
    this._chargePointService
      .getPartnerChargePoints(this.partnerID, this.currentPage, this.itemsPerPage, this.itemParams)
      .subscribe((data) => {
        if (data.result) {
          this.data = data.result;
        }
      });
  }

  // Getting Item Fields
  private _getItemFields() {
    // Ensure this.chargePoint is defined
    if (!this.chargePoint || this.chargePoint == undefined) {
      return;
    }
    // Getting item fields
    Object.keys(this.chargePoint ?? {}).forEach((element: string) => {
      console.log(element);
      if (
        typeof this.chargePoint?.[element] == 'object' &&
        this.chargePoint?.[element] != null &&
        this.chargePoint?.[element].constructor.name == 'Date'
      )
        this.fields.push(element);
      if (typeof this.chargePoint?.[element] != 'object')
        this.fields.push(element);
    });
  }

  // Deleting the item

  // Next page
  nextPage() {
    this.currentPage++;
    this.getAll();
  }

  // Previous Page
  previousPage() {
    this.currentPage--;
    this.getAll();
  }

  delete(id: number) {
    this._chargePointService.deleteById(id).subscribe((data) => {
      this.getAll();
    });
  }

  display(id: number) {
    this._router.navigate(['/charging-points', id]);
  }

  update(id: number) {
    console.log('this is update function');
  }

  sort(field: string) {
    if (this.itemParams.orderBy == field) {
      if (this.itemParams.reverseOrder == 'y')
        this.itemParams.reverseOrder = 'n';
      else this.itemParams.reverseOrder = 'y';
    } else {
      this.itemParams.orderBy = field;
      this.itemParams.reverseOrder = 'n';
    }
    this.getAll();
  }

  // Applying filters
  applyFilters() {
    this.itemParams.FilterValue = [this.filters.category, this.filters.status];
    this.itemParams.FilterBy = ['Status', 'Category'];
    this.itemParams.FilterMethod = '&&';
    this.getAll();
  }

  search(val: string) {
    // Update parameters in itemParams
    this.itemParams.SearchBy = ['Name', 'Address']; // array of fields to search in
    this.itemParams.SearchValue = 'Gut'; // the value to search for
    this.getAll();
  }

}

import { formatDate } from '@angular/common';
import { Component, ContentChild, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { PaginatedResult, Pagination } from 'src/_models/pagination';
import { ActionModalService } from 'src/_services/action-modal.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

@Component({
  selector: 'app-display-table-list',
  templateUrl: './display-table-list.component.html',
  styleUrls: ['./display-table-list.component.sass']
})
export class DisplayTableListComponent implements OnInit {
  @ContentChild(AppTableCustomButtonDirective, { static: false })
  customButtonTemplate!: AppTableCustomButtonDirective;
  @Input() name: string = "";
  @Input() names: string = "";
  @Input() fields: string[] = [];
  @Input() searchByAttributes: string[] = [];
  @Input() data: any[] = [];
  @Input() routeName : string = "";
  @Input() getItemsObservable! : (page?: number, itemsPerPage?: number, itemParams?: any, endpoint?: string) => Observable<PaginatedResult<any[]>> 
  @Output() sortEvent: EventEmitter<string> = new EventEmitter<string>();

  constructor( 
    private _enumMappingService : EnumMappingService,
    private _modalService : ActionModalService,
    private _router : Router
  ) { }

  paginationPages: any[] = [];
  
  sortedColumn : string= "";
  sortedDirection : string = "ASC";

  pagination: Pagination = {
    currentPage: 0,
    itemsPerPage: 20,
    totalItems: 0,
    totalPages: 0
  }

  itemParams : any = {};

  itemsPerPage: number = 10;
  currentPage: number = 1;
  
  isActive: boolean = false;

  fieldShown: { [key: string]: Boolean } = {};

  displayMenu: Boolean = false;



  ngOnInit() {
    this.getAll();
    var i = 0;
    this.fields.forEach((field) => {
      if (i < 5) {
        this.fieldShown[field] = true;
      }
      else {
        this.fieldShown[field] = false;
      }
      i++;
    });
  }

  getAll(){
    this.getItemsObservable(this.currentPage, this.itemsPerPage, this.itemParams).subscribe((data) => { 
      if(data.result)
        this.data = data.result;
      if(data.pagination){
        this.pagination = data.pagination;
        console.log("this is the pagination brother ");
        console.log(data.pagination);
        console.log(this.pagination)
        this.generatePaginationLinks();
      }
    })
  }

  nextPage() {
    this.currentPage++;
    this.getAll();
  }

  previousPage() {
    this.currentPage--;
    this.getAll();
  }


  capitalizeFirstLetter(str: string): string {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }

  goToPage(page : number){
    this.currentPage = page;
    this.getAll();
  }

  sort(field : string){
    if(this.itemParams.orderBy == field){
      if(this.itemParams.reverseOrder == 'y')
      this.itemParams.reverseOrder = 'n'
      else
      this.itemParams.reverseOrder = 'y'
    }
    else{
      this.itemParams.orderBy = field;
      this.itemParams.reverseOrder = 'n'
    }
    this.getAll();
  }

  toggleActive(event: Event): void {
      event.stopPropagation();
      this.isActive = !this.isActive;
  }

  removeActive(): void {
      this.isActive = false;
  }

  formatValue(item: any, field: string): any {
    let value = item[field.charAt(0).toLowerCase() + field.slice(1)];
    if(this.isDateString(value)) {
      const date = new Date(value);
      return formatDate(date, 'yyyy/MM/dd hh:mm:ss', 'en-US');
    } else if(field == 'status' && typeof value === 'number' && this.name == 'charging card') {
      const enumMapping = this._enumMappingService.getEnumMapping("CardStatus");
      return enumMapping ? enumMapping[value] ?? value : value;
    } else if(field == 'cardType') {
      const enumMapping = this._enumMappingService.getEnumMapping("CardType");
      return enumMapping ? enumMapping[value] ?? value : value;
    } else if(field == 'status' && typeof value === 'number' && this.name == 'charging station') {
      const enumMapping = this._enumMappingService.getEnumMapping("ChargingStationStatusEnum");
      return enumMapping ? enumMapping[value] ?? value : value;
    } else if(field == 'category' && typeof value === 'number' && this.name == 'charging station') {
      const enumMapping = this._enumMappingService.getEnumMapping("ChargingStationCategoryEnum");
      return enumMapping ? enumMapping[value] ?? value : value;
    } else if(field == 'parkingType' && typeof value === 'number' && this.name == 'charging station') {
      const enumMapping = this._enumMappingService.getEnumMapping("ParkingTypeEnum");
      return enumMapping ? enumMapping[value] ?? value : value;
    } else if(field == 'status' && typeof value === 'number' && this.name == 'charging point') {
      const enumMapping = this._enumMappingService.getEnumMapping("ChargePointStatus");
      return enumMapping ? enumMapping[value] ?? value : value;
    } else if(field == 'category' && typeof value === 'number' && this.name == 'charging point') {
      const enumMapping = this._enumMappingService.getEnumMapping("ChargePointCategory");
      return enumMapping ? enumMapping[value] ?? value : value;
    }

    else if(typeof value === 'number' || this.isNumericString(value)) {
      const num = Number(value);
      if (Number.isInteger(num)) {
        return num.toString();
      } else {
        return num.toFixed(2);
      }
    }
    
    else {
      return value;
    }
  }
  
  isDateString(value: any): boolean {
    const regex = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+$/;
    return typeof value === 'string' && regex.test(value);
  }
  
  isNumericString(value: any): boolean {
    return typeof value === 'string' && !isNaN(Number(value)) && value.includes(".");
  }
  
  generatePaginationLinks() {
    const currentPage = this.pagination.currentPage;
    const totalPages = this.pagination.totalPages;
    if (totalPages <= 3) {
      this.paginationPages = Array.from({ length: totalPages }, (_, i) => i + 1);
    } else if (currentPage === 1) {
      this.paginationPages = [1, 2, '...', totalPages];
    } else if (currentPage == totalPages) {
      this.paginationPages = [currentPage - 2, currentPage - 1, '...', totalPages];
    } else {
      this.paginationPages = [currentPage - 1, currentPage, '...', totalPages];
    }
    
  }
}

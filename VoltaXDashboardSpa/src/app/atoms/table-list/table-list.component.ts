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
  selector: 'app-table-list',
  templateUrl: './table-list.component.html',
  styleUrls: ['./table-list.component.sass']
})
export class TableListComponent implements OnInit {
  @ContentChild(AppTableCustomButtonDirective, { static: false })
  customButtonTemplate!: AppTableCustomButtonDirective;
  @Input() name: string = "";
  @Input() names: string = "";
  @Input() fields: string[] = [];
  @Input() searchByAttributes: string[] = [];
  @Input() data: any[] = [];
  @Input() createLink: string = "/create";
  @Input() routeName : string = "";
  @Input() searchByPlaceHolder: string = "Search by name";
  @Input() getItemsObservable! : (page?: number, itemsPerPage?: number, itemParams?: any, endpoint?: string) => Observable<PaginatedResult<any[]>> 
  @Input() deleteItemObservable! : (id : number) => Observable<any>;
  @Input() updateItemObservable! : (id : number, model : any) => Observable<any>;
  @Output() applyFiltersEvent: EventEmitter<void> = new EventEmitter<void>();
  @Output() resetFiltersEvent: EventEmitter<void> = new EventEmitter<void>();
  @Output() displayEvent: EventEmitter<number> = new EventEmitter<number>();
  @Output() updateEvent: EventEmitter<number> = new EventEmitter<number>();
  @Output() sortEvent: EventEmitter<string> = new EventEmitter<string>();


  constructor( 
    private _enumMappingService : EnumMappingService,
    private _modalService : ActionModalService,
    private _router : Router
  ) { }

  delete(id : number){
    this.deleteItemObservable(id).subscribe((data) => {
      this.getAll();
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !","Item deleted successfully ",4000);
    })
  }

  paginationPages: any[] = [];
  
  sortedColumn : string= "";
  sortedDirection : string = "ASC";

  pagination: Pagination = {
    currentPage: 0,
    itemsPerPage: 0,
    totalItems: 0,
    totalPages: 0
  }

  itemParams : any = {};

  itemsPerPage: number = 20;
  currentPage: number = 1;

  searchValue: string = "";
  private searchSubject = new Subject<string>();
  
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
    this.searchSubject.pipe(
      debounceTime(1000),
      distinctUntilChanged()
    ).subscribe(searchValue => {
      this.getAll();
    });
  }

  getAll(){
    this.getItemsObservable(this.currentPage, this.itemsPerPage, this.itemParams).subscribe((data) => {
      if(data.result)
        this.data = data.result;
      if(data.pagination){
        this.pagination = data.pagination;
        this.generatePaginationLinks();
      }
    })
  }

  nextPage() {
    this.currentPage++;
    this.getAll();
  }

  // Previous Page
  previousPage() {
    this.currentPage--;
    this.getAll();
  }

  search(){
    if(this.searchValue == null || this.searchValue == ""){
      this.itemParams.SearchBy = [];
      this.itemParams.SearchValue = [];
    }
    else{
      this.itemParams.SearchBy = this.searchByAttributes; 
      this.itemParams.SearchValue = this.searchValue;
    }
    this.getAll(); 
  }

  applyFilters() {
    this.itemParams.FilterValue = [];
    this.itemParams.FilterBy = [];
  
    for (const key in this.filters) {
      if (this.filters[key]) {  
        this.itemParams.FilterValue.push(this.filters[key]);  
        this.itemParams.FilterBy.push(this.capitalizeFirstLetter(key));  
      }
    }
    console.log("this is the filters");
    console.log(this.filters);
    this.getAll();  
  }

  capitalizeFirstLetter(str: string): string {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }

  @Input() filters : any = {};

  update = (id: number) =>  this.updateEvent.emit(id);

  display(id: number){
    this._router.navigateByUrl("/dashboard/"+this.routeName+"/"+id)
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
      // If it's a date string, parse it as a date and format it
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
      // If it's a number (or a string that can be parsed as a number), format it with 2 decimal places
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
  
  resetFilters(){
    this.resetFiltersEvent.emit();
  }
}

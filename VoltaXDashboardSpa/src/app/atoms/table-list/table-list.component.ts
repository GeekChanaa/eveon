import { formatDate } from '@angular/common';
import { Component, ContentChild, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

@Component({
  selector: 'app-table-list',
  templateUrl: './table-list.component.html',
  styleUrls: ['./table-list.component.css']
})
export class TableListComponent implements OnInit {
  @ContentChild(AppTableCustomButtonDirective, { static: false })
  customButtonTemplate!: AppTableCustomButtonDirective;
  @Input() name: string = "";
  @Input() names: string = "";
  @Input() fields: string[] = [];
  @Input() data: any[] = [];
  @Input() createLink: string = "/";
  @Input() searchByPlaceHolder: string = "Search by name";
  @Output() next: EventEmitter<void> = new EventEmitter<void>();
  @Output() previous: EventEmitter<void> = new EventEmitter<void>();
  @Output() firstPage: EventEmitter<void> = new EventEmitter<void>();
  @Output() lastPage: EventEmitter<void> = new EventEmitter<void>();
  @Output() applyFiltersEvent: EventEmitter<void> = new EventEmitter<void>();
  @Output() deleteEvent: EventEmitter<number> = new EventEmitter<number>();
  @Output() displayEvent: EventEmitter<number> = new EventEmitter<number>();
  @Output() updateEvent: EventEmitter<number> = new EventEmitter<number>();
  @Output() sortEvent: EventEmitter<string> = new EventEmitter<string>();
  @Output() searchEvent: EventEmitter<string> = new EventEmitter<string>();

  searchValue: string = "";
  private searchSubject = new Subject<string>();
  
  isActive: boolean = false;

  fieldShown: { [key: string]: Boolean } = {};

  displayMenu: Boolean = false;

  constructor( 
    private _enumMappingService : EnumMappingService
  ) { }

  ngOnInit() {
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
      this.searchEvent.emit(searchValue);
    });
  }

  // next Page Event
  nextPage() {
    this.next.emit();
  }

  // first Page Event
  firstP() {
    this.firstPage.emit();
  }

  // last Page Event
  lastP() {
    this.lastPage.emit();
  }

  // previous page event
  previousPage() {
    this.previous.emit();
  }

  // delete function
  delete(id: number) {
    this.deleteEvent.emit(id);
  }

  // update 
  update(id: number) {
    this.updateEvent.emit(id);
  }

  //display 
  display(id: number) {
    this.displayEvent.emit(id);
  }

  // sorting by field
  sort(field: string) {
    this.sortEvent.emit(field);
  } 

  // search field
  search() {
    this.searchSubject.next(this.searchValue);
  }


  toggleActive(event: Event): void {
      event.stopPropagation();
      this.isActive = !this.isActive;
  }

  removeActive(): void {
      this.isActive = false;
  }

  applyFilters(){
    this.applyFiltersEvent.emit();
    this.removeActive();
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
  
  
}

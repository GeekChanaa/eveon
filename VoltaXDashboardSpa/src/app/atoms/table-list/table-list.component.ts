import { Component, ContentChild, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';

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

  constructor() { }

  ngOnInit() {
    var i = 0
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
}

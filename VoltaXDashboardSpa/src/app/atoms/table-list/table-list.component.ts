import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-table-list',
  templateUrl: './table-list.component.html',
  styleUrls: ['./table-list.component.css']
})
export class TableListComponent implements OnInit {

  @Input() name : string = "";
  @Input() names : string = "";
  @Input() fields : string[] = [];
  @Input() data : any[] = [];
  @Output() next : EventEmitter<void> = new EventEmitter<void>();
  @Output() previous : EventEmitter<void> = new EventEmitter<void>();
  @Output() firstPage : EventEmitter<void> = new EventEmitter<void>();
  @Output() lastPage : EventEmitter<void> = new EventEmitter<void>();
  @Output() deleteEvent : EventEmitter<number> = new EventEmitter<number>();
  @Output() displayEvent : EventEmitter<number> = new EventEmitter<number>();
  @Output() updateEvent : EventEmitter<number> = new EventEmitter<number>();
  fieldShown : { [key: string]: Boolean } = {};

  displayMenu : Boolean = false;

  constructor() { }

  ngOnInit() {
    var i = 0
    this.fields.forEach((field) => {
      if(i<5){
        this.fieldShown[field] = true;
      }
      else{
        this.fieldShown[field] = false;
      }
      i++;
    });
    console.log(this.fieldShown);
  }

  // next Page Event
  nextPage(){
    this.next.emit();
  }

  // first Page Event
  firstP(){
    this.firstPage.emit();
  }
  
  // last Page Event
  lastP(){
    this.lastPage.emit();
  }

  // previous page event
  previousPage(){
    this.previous.emit();
  }

  // delete function
  delete(id : number){
    this.deleteEvent.emit(id);
  }

  // update 
  update(id : number){
    this.updateEvent.emit(id);
  }

  //display 
  display(id : number){
    this.displayEvent.emit(id);
  }
}

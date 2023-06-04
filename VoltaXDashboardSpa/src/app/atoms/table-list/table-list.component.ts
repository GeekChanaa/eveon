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

  constructor() { }

  ngOnInit() {
  }

  // next Page Event
  nextPage(){
    this.next.emit();
  }

  // previous page event
  previousPage(){
    this.previous.emit();
  }

}

import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-display-cell',
  templateUrl: './display-cell.component.html',
  styleUrls: ['./display-cell.component.css']
})
export class DisplayCellComponent implements OnInit {

  @Input() title : any = {};
  @Input() val : any = {};

  // constructor
  constructor() { }

  ngOnInit() {
  }

}

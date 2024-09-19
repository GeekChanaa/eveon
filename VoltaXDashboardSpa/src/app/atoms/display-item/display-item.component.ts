import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-display-item',
  templateUrl: './display-item.component.html',
  styleUrls: ['./display-item.component.sass']
})
export class DisplayItemComponent implements OnInit {

  @Input() name : string= "";
  @Input() value : string= "";
  @Input() tooltip : string= "";

  constructor() { }

  ngOnInit() {
  }

}

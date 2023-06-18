import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-small-card',
  templateUrl: './small-card.component.html',
  styleUrls: ['./small-card.component.css']
})
export class SmallCardComponent implements OnInit {

  @Input() title : string = "";
  @Input() description : string = "";
  @Input() number : number = 0;



  constructor() { }

  ngOnInit() {
  }

}

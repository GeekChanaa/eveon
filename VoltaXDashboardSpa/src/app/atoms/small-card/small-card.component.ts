import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-small-card',
  templateUrl: './small-card.component.html',
  styleUrls: ['./small-card.component.sass']
})
export class SmallCardComponent implements OnInit {

  @Input() title : string = "";
  @Input() description : string = "";
  @Input() icon : string = "";
  @Input() number : number = 0;



  constructor() { }

  ngOnInit() {
  }

}

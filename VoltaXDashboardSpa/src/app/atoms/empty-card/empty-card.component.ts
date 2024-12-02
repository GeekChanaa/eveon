import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-empty-card',
  templateUrl: './empty-card.component.html',
  styleUrls: ['./empty-card.component.sass']
})
export class EmptyCardComponent implements OnInit {

  @Input() imgSrc : string = "/assets/images/empty.svg";
  @Input() action : string = "";
  @Input() actionName : string = "";

  constructor() { }

  ngOnInit() {
  }

}

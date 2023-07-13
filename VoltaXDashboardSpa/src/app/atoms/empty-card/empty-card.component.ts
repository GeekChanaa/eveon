import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-empty-card',
  templateUrl: './empty-card.component.html',
  styleUrls: ['./empty-card.component.css']
})
export class EmptyCardComponent implements OnInit {

  @Input() imgSrc : string = "/assets/img/content/login-pic.png";

  constructor() { }

  ngOnInit() {
  }

}

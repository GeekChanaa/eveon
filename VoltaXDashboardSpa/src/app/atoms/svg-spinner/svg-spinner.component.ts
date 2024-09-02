import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-svg-spinner',
  templateUrl: './svg-spinner.component.html',
  styleUrls: ['./svg-spinner.component.sass']
})
export class SvgSpinnerComponent implements OnInit {
  @Input() size : number= 30;

  constructor() { }

  ngOnInit() {
  }

}

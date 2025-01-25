import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-set-variables-request',
  templateUrl: './set-variables-request.component.html',
  styleUrls: ['./set-variables-request.component.sass']
})
export class SetVariablesRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}

import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-connector-status-dot',
  templateUrl: './connector-status-dot.component.html',
  styleUrls: ['./connector-status-dot.component.sass']
})
export class ConnectorStatusDotComponent implements OnInit {

  @Input() status : string = "disconnected";

  constructor() { }

  ngOnInit() {
  }

}

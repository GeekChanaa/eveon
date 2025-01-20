import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-connector-status-tag',
  templateUrl: './connector-status-tag.component.html',
  styleUrls: ['./connector-status-tag.component.sass']
})
export class ConnectorStatusTagComponent implements OnInit {
  
  @Input() connectorStatus: string = '';
  constructor() { }

  ngOnInit() {
  }

}

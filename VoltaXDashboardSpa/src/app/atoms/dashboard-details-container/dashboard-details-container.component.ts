import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-dashboard-details-container',
  templateUrl: './dashboard-details-container.component.html',
  styleUrls: ['./dashboard-details-container.component.sass']
})
export class DashboardDetailsContainerComponent implements OnInit {

  @Input() title : string = "";
  @Input() breadcrumbs: { label: string, link?: string }[] = [];
  

  constructor() { }

  ngOnInit() {
  }


}

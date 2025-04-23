import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-dashboard-form-container',
  templateUrl: './dashboard-form-container.component.html',
  styleUrls: ['./dashboard-form-container.component.sass']
})
export class DashboardFormContainerComponent implements OnInit {

  @Input() title : string = "";
  @Input() items: { label: string, link?: string }[] = [];

  constructor() { }

  ngOnInit() {
  }

}

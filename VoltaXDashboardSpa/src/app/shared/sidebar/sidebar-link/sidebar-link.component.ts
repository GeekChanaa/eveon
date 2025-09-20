import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-sidebar-link',
  templateUrl: './sidebar-link.component.html',
  styleUrls: ['./sidebar-link.component.sass']
})
export class SidebarLinkComponent implements OnInit {

  // Input properties
  @Input() title : string = "";
  @Input() link : string = "";

  @Input() active:  boolean = false;

  constructor() { }

  ngOnInit() {
  }

}

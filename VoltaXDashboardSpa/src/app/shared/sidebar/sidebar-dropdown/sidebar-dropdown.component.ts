import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-sidebar-dropdown',
  templateUrl: './sidebar-dropdown.component.html',
  styleUrls: ['./sidebar-dropdown.component.css']
})
export class SidebarDropdownComponent implements OnInit {

  // Input properties
  @Input() title : string = "";
  
  constructor() { }

  ngOnInit() {
  }

}

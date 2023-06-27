import { AfterViewInit, Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';

@Component({
  selector: 'app-sidebar-dropdown',
  templateUrl: './sidebar-dropdown.component.html',
  styleUrls: ['./sidebar-dropdown.component.css']
})
export class SidebarDropdownComponent implements OnInit, AfterViewInit {

  // Input properties
  @Input() title : string = "";
  @ViewChild('itemDropDown') itemDropDown!: ElementRef;
  
  
  constructor() { 
  }

  ngAfterViewInit() {
    this.itemDropDown.nativeElement.addEventListener('click', (event: any) => this.itemClicked(event, this.itemDropDown));
  }

  ngOnInit() {
  }

  itemClicked(event: Event, item: ElementRef) {
    event.stopPropagation();
    item.nativeElement.classList.toggle('active');
  }
  

}

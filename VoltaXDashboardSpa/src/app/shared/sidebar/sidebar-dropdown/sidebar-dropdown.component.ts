import { AfterViewInit, Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';

@Component({
  selector: 'app-sidebar-dropdown',
  templateUrl: './sidebar-dropdown.component.html',
  styleUrls: ['./sidebar-dropdown.component.sass']
})
export class SidebarDropdownComponent implements OnInit, AfterViewInit {
  @Input() active : boolean = false;
  @Input() title : string = "";
  @Input() icon : string = "";
  @Input() iconID : string = "";
  
  @ViewChild('itemDropDown') itemDropDown!: ElementRef;
  @ViewChild('topButton') topButton!: ElementRef; // Reference to the 'sidebar__top'

  constructor() { }
  ngOnInit() {
  }

  ngAfterViewInit() {
    this.topButton.nativeElement.addEventListener('click', (event: any) => this.itemClicked(event, this.itemDropDown));
  }

  itemClicked(event: Event, item: ElementRef) {
    event.stopPropagation();
    item.nativeElement.classList.toggle('active');
  }
  

}

import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-info-box',
  templateUrl: './info-box.component.html',
  styleUrls: ['./info-box.component.sass']
})
export class InfoBoxComponent implements OnInit {

  @Input() type: 'error' | 'warning' | 'primary' = 'primary';
  @Input() message: string = '';
  
  constructor() { }

  ngOnInit() {
  }

}

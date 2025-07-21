import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-info-item',
  templateUrl: './info-item.component.html',
  styleUrls: ['./info-item.component.sass']
})
export class InfoItemComponent implements OnInit {

  @Input() title!: string;
  @Input() value!: string | null;
  @Input() tooltip: string = 'Maximum 100 characters. No HTML or emoji allowed';
  
  constructor() { }

  ngOnInit() {
  }

}

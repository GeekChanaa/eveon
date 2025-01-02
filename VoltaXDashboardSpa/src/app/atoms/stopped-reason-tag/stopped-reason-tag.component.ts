import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-stopped-reason-tag',
  templateUrl: './stopped-reason-tag.component.html',
  styleUrls: ['./stopped-reason-tag.component.sass']
})
export class StoppedReasonTagComponent implements OnInit {

  @Input() stoppedReason : string = "";
  
  constructor() { }

  ngOnInit() {
  }

}

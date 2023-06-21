import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-add-charge-point',
  templateUrl: './add-charge-point.component.html',
  styleUrls: ['./add-charge-point.component.css']
})
export class AddChargePointComponent implements OnInit {

  // charge points
  chargePoints : any[] = [];

  // Constructor
  constructor() { }

  // On init CycleHook
  ngOnInit() {
  }

}

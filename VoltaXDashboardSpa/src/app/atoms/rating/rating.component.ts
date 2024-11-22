import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-rating',
  templateUrl: './rating.component.html',
  styleUrls: ['./rating.component.sass']
})
export class RatingComponent implements OnInit {

  @Input() score : number = 1;

  get filledStars() {
    return new Array(5).fill(false).map((_, i) => i < this.score); 
  }

  constructor() { }

  ngOnInit() {
  }

}

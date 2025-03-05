import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-good-bye',
  templateUrl: './good-bye.component.html',
  styleUrls: ['./good-bye.component.sass']
})
export class GoodByeComponent implements OnInit {

  constructor(private router: Router) {}

  ngOnInit(): void {
    setTimeout(() => {
      this.router.navigate(['/']); 
    }, 5000);
  }

}

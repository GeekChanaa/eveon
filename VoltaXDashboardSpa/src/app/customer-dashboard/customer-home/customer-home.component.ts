import {AfterViewInit, Component, ElementRef, OnInit, ViewChild} from '@angular/core';
import { AuthService } from 'src/_services/auth.service';
import { CardService } from 'src/_services/card.service';
import { UserService } from 'src/_services/user.service';
@Component({
  selector: 'app-customer-home',
  templateUrl: './customer-home.component.html',
  styleUrls: ['./customer-home.component.css']
})
export class CustomerHomeComponent implements OnInit {

  constructor(
    private _userService : UserService,
    private _authService : AuthService,
    private _cardService: CardService
  ) {}

  // recharge cards : 
  rechargeCards : any[] = [];

  ngOnInit(): void {
    var id = this._authService.getAuthInformation().nameid;
    this._cardService.getUserRechargeCards(id).subscribe((data) => {
      this.rechargeCards = data;
    })
  }

  // Slider Configurtion
  slides = [
    { img: 'https://via.placeholder.com/600.png/09f/fff' },
    { img: 'https://via.placeholder.com/600.png/021/fff' },
    { img: 'https://via.placeholder.com/600.png/321/fff' },
    { img: 'https://via.placeholder.com/600.png/422/fff' },
    { img: 'https://via.placeholder.com/600.png/654/fff' },
  ];
  slideConfig = { slidesToShow: 1, slidesToScroll: 1 };

  addSlide() {
    this.slides.push({ img: 'http://placehold.it/350x150/777777' });
  }

  removeSlide() {
    this.slides.length = this.slides.length - 1;
  }

  slickInit(e: any) {
    console.log('slick initialized');
  }

  breakpoint(e: any) {
    console.log('breakpoint');
  }

  afterChange(e: any) {
    console.log('afterChange');
  }

  beforeChange(e: any) {
    console.log('beforeChange');
  }

  

}

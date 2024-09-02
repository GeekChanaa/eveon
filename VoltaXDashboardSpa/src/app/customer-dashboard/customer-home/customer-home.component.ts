import {AfterViewInit, Component, ElementRef, HostListener, OnInit, Renderer2, ViewChild} from '@angular/core';
import { UserRole } from 'src/_models/_enums/user-role';
import { User } from 'src/_models/user';
import { AuthService } from 'src/_services/auth.service';
import { CardService } from 'src/_services/card.service';
import { UserService } from 'src/_services/user.service';
@Component({
  selector: 'app-customer-home',
  templateUrl: './customer-home.component.html',
  styleUrls: ['./customer-home.component.css']
})
export class CustomerHomeComponent implements OnInit {

  loggedInUserFirstName : string = "";
  loggedInUserLastName : string = "";

  loggedInUser : User={
    id: 0,
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    role: "Customer",
    isEmailVerified: false,
    isPhoneVerified: false
  }

  constructor(
    private _userService : UserService,
    private _authService : AuthService,
    private _cardService: CardService,
    private el: ElementRef, 
    private renderer: Renderer2
  ) {}

  // recharge cards : 
  rechargeCards : any[] = [];

  ngOnInit(): void {
    var info = this._authService.getAuthInformation();
    var id = this._authService.getAuthInformation().nameid;
    this._cardService.getUserRechargeCards(id).subscribe((data) => {
      this.rechargeCards = data;
    });
    this._userService.getById(id).subscribe((data) => {
      this.loggedInUser = data;
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

  public popupElement: any;
  public bodyElement: HTMLElement = document.getElementsByTagName('body')[0];

  ngAfterViewInit() {
    // Assume all the popup triggers, overlays, and close buttons are marked with an appropriate class in the template
    const triggers = this.el.nativeElement.querySelectorAll('[data-popup]');
    const overlays = this.el.nativeElement.querySelectorAll('.js-popup-overlay');
    const closeButtons = this.el.nativeElement.querySelectorAll('.js-popup-close');

    triggers.forEach((trigger : any) => {
      this.renderer.listen(trigger, 'click', (event) => {
        this.popupElement = this.el.nativeElement.querySelector(trigger.getAttribute('data-popup'));
        this.showPopup(this.popupElement);
      });
    });

    closeButtons.forEach((button : any) => {
      this.renderer.listen(button, 'click', (event) => {
        this.popupElement = button.closest('.js-popup');
        this.hidePopup();
      });
    });

    overlays.forEach((overlay : any) => {
      this.renderer.listen(overlay, 'click', (event) => {
        this.popupElement = overlay.closest('.js-popup');
        this.hidePopup();
      });
    });
    
  }

  @HostListener('document:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      this.hidePopup();
    }
  }

  showPopup(element : any) {
    this.renderer.addClass(element, 'animation');
    this.renderer.addClass(element, 'visible');
    this.renderer.addClass(this.bodyElement, 'no-scroll');
  }

  hidePopup() {
    if (this.popupElement) {
      this.renderer.removeClass(this.popupElement, 'animation');

      if (document.querySelectorAll('.js-popup.visible').length === 1) {
        this.renderer.removeClass(this.bodyElement, 'no-scroll');
      }

      setTimeout(() => {
        this.renderer.removeClass(this.popupElement, 'visible');
      }, 300);
    }
  }

}

import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-enter-phone-number',
  templateUrl: './enter-phone-number.component.html',
  styleUrls: ['./enter-phone-number.component.css']
})
export class EnterPhoneNumberComponent implements OnInit {

  form : FormGroup;

  // Constructor
  constructor(
    private _authService : AuthService,
    private _route : ActivatedRoute
  ) { 
    this.form = new FormGroup({
      phone: new FormControl('', [
        Validators.required,
        Validators.pattern('^((\\+91-?)|0)?[0-9]{10}$') // adjust pattern as needed
      ])
    })
  }

  // on init cycle hook
  ngOnInit() {
  }

  sendPhoneVerification(){
    
  }

}

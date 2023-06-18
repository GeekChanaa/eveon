import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { UserForRegisterDto } from 'src/_models/_dtos/user-for-register-dto';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-sign-up',
  templateUrl: './sign-up.component.html',
  styleUrls: ['./sign-up.component.css']
})
export class SignUpComponent implements OnInit {

  form : FormGroup;

  // constructor
  constructor( private _authService : AuthService) { 
    this.form = new FormGroup({
      firstName : new FormControl(''),
      lastName : new FormControl(''),
      email : new FormControl(''),
      phone : new FormControl(''),
      password : new FormControl('')
    })
  }

  // On init cycle hook
  ngOnInit() {
  }

  // sign up function
  register(){
    const formValue = this.form.value;
    var userForRegister : UserForRegisterDto = {
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      email: formValue.email,
      phone: formValue.phone,
      password: formValue.password
    }

    this._authService.register(userForRegister).subscribe((data) => {
      console.log("user registered success");
    })
  }

}

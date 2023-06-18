import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { UserForLoginDto } from 'src/_models/_dtos/user-for-login-dto';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {

  form: FormGroup;
  constructor(
    private _authService: AuthService
  ) {
    this.form = new FormGroup({
      email: new FormControl(''),
      password: new FormControl('')
    })
  }

  // On init cycle hook
  ngOnInit() {
  }

  // Login button
  login(){
    const formValue = this.form.value;
    var userForLogin : UserForLoginDto = {
      email : formValue.email,
      password : formValue.password
    };
    this._authService.login(userForLogin).subscribe((data) => {
      console.log("user login success");
    })
  }

}

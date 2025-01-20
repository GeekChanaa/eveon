import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-request-password',
  templateUrl: './request-password.component.html',
  styleUrls: ['./request-password.component.sass']
})
export class RequestPasswordComponent implements OnInit {

  email : string = "";
  isLoading : boolean = false;
  form: FormGroup;
  errorMessage : string = "";

  constructor(
    private _authService: AuthService
  ) { 
    this.form = new FormGroup({
      email: new FormControl('', [
        Validators.required,
        Validators.email
      ])
    });
  }

  // On init cycle hook
  ngOnInit() {
  }

  resetPasswordRequest(){
    this.isLoading = true;
    this._authService.resetPasswordRequest(this.form.value.email).subscribe(data => {
      this.isLoading = false;
    },(error) => {
      this.isLoading = false;
      console.log(error);
      console.log(error.error);
      this.errorMessage = error.error.error;
    });
  }

}

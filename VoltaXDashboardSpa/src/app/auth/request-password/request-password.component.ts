import { Component, OnInit } from '@angular/core';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-request-password',
  templateUrl: './request-password.component.html',
  styleUrls: ['./request-password.component.sass']
})
export class RequestPasswordComponent implements OnInit {

  email : string = "";
  isLoading : boolean = false;

  constructor(
    private _authService: AuthService
  ) { }

  // On init cycle hook
  ngOnInit() {
  }

  resetPasswordRequest(){
    this.isLoading = true;
    this._authService.resetPasswordRequest(this.email).subscribe(data => {
      this.isLoading = false;
      console.log(data);  
    },(error) => {
      this.isLoading = false;
    });
  }

}

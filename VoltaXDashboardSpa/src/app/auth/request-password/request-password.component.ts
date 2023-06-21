import { Component, OnInit } from '@angular/core';
import { AuthService } from 'src/_services/auth.service';

@Component({
  selector: 'app-request-password',
  templateUrl: './request-password.component.html',
  styleUrls: ['./request-password.component.css']
})
export class RequestPasswordComponent implements OnInit {

  email : string = "";

  constructor(
    private _authService: AuthService
  ) { }

  // On init cycle hook
  ngOnInit() {
  }

  resetPasswordRequest(){
    this._authService.resetPasswordRequest(this.email).subscribe(data => {
      console.log(data);
    });
  }

}

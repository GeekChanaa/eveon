import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from 'src/_services/auth.service';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';

@Component({
  selector: 'app-verify-phone',
  templateUrl: './verify-phone.component.html',
  styleUrls: ['./verify-phone.component.sass']
})
export class VerifyPhoneComponent implements OnInit {

  form: FormGroup;
  email: string = "";
  isLoading: boolean = false;
  errorMessage: string = "";

  constructor(
    private _authService: AuthService,
    private _router: Router,
    private _modalService: ActionModalService
  ) { 
    this.form = new FormGroup({
      code: new FormControl('', [Validators.required])
    });
  }

  ngOnInit() {
    this.email = this._authService.getAuthInformation().unique_name;
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  verify() {
    if (!this.form.valid) return;

    this.isLoading = true;
    const token = this.form.value.code;

    const verifyPhoneDto = {
      token: token,
      email: this.email
    };

    this._authService.verifyPhone(verifyPhoneDto).subscribe({
      next: () => {
        this.isLoading = false;
        this._router.navigate(['/my-dashboard']);
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error?.error || "Something went wrong";
        this._modalService.popup(
          ActionModalStatusEnum.Error,
          "Error!",
          this.errorMessage,
          4000
        );
      }
    });
  }
}

import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { User } from 'src/_models/user';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { FileManagementService } from 'src/_services/file-management.service';
import { UserService } from 'src/_services/user.service';
import { environment } from 'src/environments/environment';


@Component({
  selector: 'app-account-informations',
  templateUrl: './account-informations.component.html',
  styleUrls: ['./account-informations.component.sass']
})
export class AccountInformationsComponent implements OnInit {
editingImage: boolean = false;
  imageUploading: boolean = false;
  displayedImage: string | null = null;
  selectedFile: File | null = null;

  isSendingEmail : boolean = false;
  isSendingPhoneSms : boolean = false;

  updateEmailForm : FormGroup;

  isChangeAvatarModalOpen : boolean = false;

  // Editing bools
  editingFirstName : Boolean = false;
  editingLastName : Boolean = false;
  editingEmail : Boolean = false;
  editingPhone : Boolean = false;
  verifyingEmail : Boolean = false;
  verifyingPhone : Boolean = false;
  editingPassword : Boolean = false;
  staticUrl : string = environment.apiStaticFilesUrl;

  showVerificationModal = false;
  verificationType: 'email' | 'phone' = 'email';
  verificationStep = 1;
  verificationCode = '';
  verificationError = '';
  verifying = false;

  //userid
  userID : number = 0;

  // Profile Picture
  profilePicture : any = {};


  // CurrentUser
  user : User = {
    firstName: '',
    lastName: '',
    id: 0,
    email: '',
    phone: '',
    role: "Customer",
    isEmailVerified: false,
    isPhoneNumberVerified: false,
    imageUrl: ''
  };

  updateUserObservable = (id : number, model : any) => this._userService.editUserDashboardInformations(id, model);

  // Constructor
  constructor(
    private _authService : AuthService,
    private _userService : UserService,
    private _fileManagementService : FileManagementService,
    private _modalService : ActionModalService
  ) {    
    this.updateEmailForm = new FormGroup({
      email : new FormControl('',[Validators.required]),
    })
  }

  // On init cycle hook
  ngOnInit() {
    this.getAuthUserInfos();
  }

  getAuthUserInfos(){
    var decodedToken = this._authService.getAuthInformation();
    this.userID = parseInt(decodedToken.nameid);
    this.getUserByID(this.userID);
  }

  

  getUserByID(id : number ){
    this._userService.getUserInformations(id).subscribe((data) => {
      this.user = data;
      console.log("this is the user data");
      console.log(this.user);
      this._userService.setAvatarUrl(this.user.imageUrl);
    })
  }

  openChangeAvatarModal(){
    this.isChangeAvatarModalOpen = true;
  }

  changeAvatar(): void {
    if (!this.selectedFile) return;


    this.imageUploading = true;
    const formData = new FormData();
    formData.append('imageFile', this.selectedFile);

    this._userService.uploadUserAvatar(formData, this.userID).subscribe((data) => {
      this.imageUploading = false;
      this.editingImage = false;
      this.getUserByID(this.userID);
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !", "Image Uploaded Successfully ! ",4000);
      this.isChangeAvatarModalOpen = false;
    },(error) => {
      this.imageUploading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something Went wrong please try again later", 4000);
    })
  }

  handleUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];

      // Preview the image
      const reader = new FileReader();
      reader.onload = () => {
        this.displayedImage = reader.result as string;
      };
      reader.readAsDataURL(this.selectedFile);
    }
  }

  clearImage(): void {
    this.displayedImage = null;
    this.selectedFile = null;
  }

  closeModal(){
    this.isChangeAvatarModalOpen = false;
  }

  saveEmail(): void {
    this._userService.updateEmail({id : this.user.id, email : this.user.email}).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !", "Email Changed ! ",4000);
    })
  }

  // Save edited phone
  savePhone(): void {
  }

  // Open verification modal
  openVerificationModal(type: 'email' | 'phone'): void {
    this.verificationType = type;
    this.verificationStep = 1;
    this.verificationCode = '';
    this.verificationError = '';
    this.showVerificationModal = true;
  }

  closeVerificationModal(): void {
    this.showVerificationModal = false;
  }

  sendVerificationCode(): void {
    const contactInfo = this.verificationType === 'email' ? this.user.email : this.user.phone;
    
    
  }

  // Verify the entered code
  verifyCode(): void {
    if (!this.verificationCode) {
      this.verificationError = 'Please enter the verification code';
      return;
    }
    
    this.verifying = true;
    const contactInfo = this.verificationType === 'email' ? this.user.email : this.user.phone;
    
    
  }

  // Resend verification code
  resendCode(): void {
    const contactInfo = this.verificationType === 'email' ? this.user.email : this.user.phone;
    
  }

  verifyEmail(){
    if(this.user.isEmailVerified)
        return;
    this.isSendingEmail = true;
    this.verifyingEmail = true;
    this._authService.sendEmailVerificationCode(this.userID).subscribe((data) =>{
      this.isSendingEmail = false;
    })
  }

  verifyPhone(){
    if(this.user.isPhoneNumberVerified)
        return;
    this.isSendingPhoneSms = true;
    this.verifyingPhone = true;
    this._authService.sendPhoneVerificationSms({email : this.user.email, phone : this.user.phone}).subscribe((data) =>{
      this.isSendingPhoneSms = false;
    })
  }

}

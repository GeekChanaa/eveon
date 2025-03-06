import { Component, OnInit } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { User } from 'src/_models/user';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { FileManagementService } from 'src/_services/file-management.service';
import { UserService } from 'src/_services/user.service';
import { environment } from 'src/environments/environment';


enum ProfilePageTabsEnum {
  AccountInformationsTab = "AccountInformationsTab",
  SecurityTab = "SecurityTab",
  RechargeCardsTab = "RechargeCardsTab",
  PaymentCardsTab = "PaymentCardsTab",
  NotificationSettingsTab = "NotificationSettingsTab"
}

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.sass']
})
export class ProfileComponent implements OnInit {
  editingImage: boolean = false;
  imageUploading: boolean = false;
  displayedImage: string | null = null;
  selectedFile: File | null = null;

  isChangeAvatarModalOpen : boolean = false;

  // Editing bools
  editingFirstName : Boolean = false;
  editingLastName : Boolean = false;
  editingEmail : Boolean = false;
  editingPhone : Boolean = false;
  editingPassword : Boolean = false;
  staticUrl : string = environment.apiStaticFilesUrl;

  //userid
  userID : number = 0;

  // Profile Picture
  profilePicture : any = {};


  // TabsEnum
  tabsEnum : ProfilePageTabsEnum = ProfilePageTabsEnum.AccountInformationsTab;


  // CurrentUser
  user : User = {
    firstName: '',
    lastName: '',
    id: 0,
    email: '',
    phone: '',
    role: "Customer",
    isEmailVerified: false,
    isPhoneVerified: false,
    imageUrl: ''
  };

  updateUserObservable = (id : number, model : any) => this._userService.edit(id, model);

  // Constructor
  constructor(
    private _authService : AuthService,
    private _userService : UserService,
    private _fileManagementService : FileManagementService,
    private _modalService : ActionModalService
  ) {    
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

  changeTab(tab : any){
    this.tabsEnum = tab;
  }

  getUserByID(id : number ){
    this._userService.getUserInformations(id).subscribe((data) => {
      this.user = data;
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

}

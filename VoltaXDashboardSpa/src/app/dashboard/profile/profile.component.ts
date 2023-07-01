import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { UserRole } from 'src/_models/_enums/user-role';
import { DebitCard } from 'src/_models/debit-card';
import { User } from 'src/_models/user';
import { AuthService } from 'src/_services/auth.service';
import { DebitCardService } from 'src/_services/debit-card.service';
import { FileManagementService } from 'src/_services/file-management.service';
import { UserService } from 'src/_services/user.service';
enum ProfilePageTabsEnum {
  AccountInformationsTab = "AccountInformationsTab",
  SecurityTab = "SecurityTab",
  RechargeCardsTab = "RechargeCardsTab",
  PaymentCardsTab = "PaymentCardsTab",
}

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  // Editing bools
  editingFirstName : Boolean = false;
  editingLastName : Boolean = false;
  editingEmail : Boolean = false;
  editingPhone : Boolean = false;
  editingPassword : Boolean = false;

  //userid
  userID : number = 0;

  // Profile Picture
  profilePicture : any = {};

  showDCForm : Boolean = false;

  // Forms : 
  debitCardForm : FormGroup;

  // debit cards array : 
  debitCards : any[] = [];

  // TabsEnum
  tabsEnum : ProfilePageTabsEnum = ProfilePageTabsEnum.AccountInformationsTab;


  // CurrentUser
  user : User = {
    firstName: '',
    lastName: '',
    id: 0,
    email: '',
    phone: '' ,
    role : UserRole.Customer
  };

  // Constructor
  constructor(
    private _authService : AuthService,
    private _userService : UserService,
    private _debitCardService : DebitCardService,
    private _fileManagementService : FileManagementService
  ) {
    var decodedToken = this._authService.getAuthInformation();
    console.log("this is the profile userID : " + this.userID);
    this.debitCardForm = new FormGroup({
      debitCardName: new FormControl('', [Validators.required]),
      debitCardNumber: new FormControl('', [Validators.required, Validators.pattern(/^\d{16}$/)]),
      debitCardExpirationDate: new FormControl('', [Validators.required]),
      debitCardCVV: new FormControl('', [Validators.required, Validators.pattern(/^\d{3}$/)]),
    });
   }

  // On init cycle hook
  ngOnInit() {
    this.getAuthUserInfos();
    this.getUserDebitCards();
  }

  // Getting authenticated user informations
  getAuthUserInfos(){
    var decodedToken = this._authService.getAuthInformation();
    var userid = parseInt(decodedToken.nameid);

    this._userService.getById(userid).subscribe((user) => {
      this.user = user;
    });
  }

  // Get User Debit cards
  getUserDebitCards(){
    var decodedToken = this._authService.getAuthInformation();
    var userid = parseInt(decodedToken.nameid);
    this._userService.getUserDebitCards(userid).subscribe((data) => {
      console.log("user debit cards");
      console.log(data);
      this.debitCards = data;
      console.log(this.debitCards);
      console.log("debit cards");
    })
  }


  // Changing current tab
  changeTab(tab : any){
    this.tabsEnum = tab;
  }

  // Save Debit Card
  debitCardSave(){
    var debitCardValue = this.debitCardForm.value;
    var debitCard : DebitCard = {
      id: 0,
      userID: this.user.id,
      cardNumber: debitCardValue.debitCardNumber,
      name: debitCardValue.debitCardName,
      cvv: debitCardValue.debitCardCVV,
      expirationDate: this.convertToDate(debitCardValue.debitCardExpirationDate)
    }
    console.log(debitCard);
    this._debitCardService.create(debitCard).subscribe((data) => {
      console.log("debit card added successfully");
    })
  }
  convertToDate(dateString: string): Date {
    // Split the string into month and year
    const parts = dateString.split('/');
    const month = parseInt(parts[0], 10);
    const year = parseInt(parts[1], 10);

    // Create a new Date object
    // In JavaScript, month is zero-based, so we subtract 1 from the month.
    // Also, we're assuming the day to be 1.
    return new Date(year, month - 1, 1);
}

  // showAddDebitCardForm
  showAddDebitCardForm(){
    this.showDCForm = true;
  }

  // hideAddDebitCardForm
  hideAddDebitCardForm(){
    this.showDCForm = false;
  }

  // Upload profile picture
  uploadPicture(files : any){
    const formData = new FormData();
    this.profilePicture = <File>files[0];
    console.log(this.profilePicture);
    formData.append('imageFile',this.profilePicture, "userX.png");
    this._fileManagementService.uploadProfilePicture(formData).subscribe((data) => {
      console.log("Profile Picture successfully uploaded to the destination");
    });
  }

  addSpaces(input: string): string {
    return input.replace(/(.{4})/g, '$1 ');
  }
}

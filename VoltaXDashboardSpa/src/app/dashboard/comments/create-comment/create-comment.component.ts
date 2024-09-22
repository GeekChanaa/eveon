import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { ConnectorService } from 'src/_services/connector.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { CommentService } from 'src/_services/comment.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-create-comment',
  templateUrl: './create-comment.component.html',
  styleUrls: ['./create-comment.component.sass']
})
export class CreateCommentComponent implements OnInit {

  form : FormGroup;
  comment : any = {};
  users : any[] = [];
  connectors : any[] = [];
  chargePoints : any[] = [];
  chargingStations : any[] = [];

  ngAfterViewInit() {
  }

  ngOnInit() {
    this.getAllUserNames();
    this.getAllConnectorNames();
    this.getAllChargePointNames();
    this.getAllChargingStationNames();
  }

  opacity: number = 0;
  activeDiv = 1;



  constructor(
    private _enumService : EnumMappingService,
    private _modalService:  ActionModalService,
    private _router : Router,
    private _commentService : CommentService,
    private _userService: UserService,
    private _chargePointService: ChargePointService,
    private _connectorService : ConnectorService,
    private _chargingStationService: ChargingStationService
  ) {
    this.form = new FormGroup({
      userID : new FormControl(''),
      connectorID : new FormControl(''),
      chargePointID : new FormControl(''),
      chargingStationID : new FormControl(''),
      rating : new FormControl(''),
      text : new FormControl('')
    })

  }

  showSelect() {
    this.opacity = 1;
  }

  hideSelect() {
    this.opacity = 0;
  }

  getFormControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  
  onSubmit(){
    var commentForm = this.form.value;
    this.comment.userID = commentForm.userID;
    this.comment.connectorID = commentForm.connectorID;
    this.comment.chargePointID = commentForm.chargePointID;
    this.comment.chargingStationID = commentForm.chargingStationID;
    this.comment.rating = commentForm.rating;
    this.comment.text = commentForm.text;

    this._commentService.createComment(this.comment).subscribe((createdComment) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Comment Created Successfully",4000);
      this._router.navigateByUrl('/dashboard/comments');
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    });
  }

  

  getAllUserNames(){
    this._userService.getUserNames().subscribe((data) => {
      this.users = data;
    })
  }

  updateUser(user : any){
    this.userControl.setValue(user.id);
  }

  get userControl(): FormControl {
    const control = this.form.get('userID');
    if (!control) {
      throw new Error('User control not found');
    }
    return control as FormControl;
  }



  getAllChargePointNames(){
    this._chargePointService.getChargePointIds().subscribe((data) => {
      this.chargePoints = data;
    })
  }

  getAllChargingStationNames(){
    this._chargingStationService.getChargingStationNames().subscribe((data) => {
      console.log("CHARGING STATIONS")
      this.chargingStations = data;
      console.log(this.chargingStations);
    })
  }

  updateChargePoint(chargePoint : any){
    this.chargePointControl.setValue(chargePoint.id);
  }

  updateChargingStation(chargingStation : any){
    this.chargingStationControl.setValue(chargingStation.id);
  }

  get chargePointControl(): FormControl {
    const control = this.form.get('chargePointID');
    if (!control) {
      throw new Error('ChargePoint control not found');
    }
    return control as FormControl;
  }

  get chargingStationControl(): FormControl {
    const control = this.form.get('chargingStationID');
    if (!control) {
      throw new Error('ChargingStation control not found');
    }
    return control as FormControl;
  }

  getAllConnectorNames(){
    this._connectorService.getConnectorIds().subscribe((data) => {
      this.connectors = data;
    })
  }

  updateConnector(connector : any){
    this.connectorControl.setValue(connector.id);
  }

  get connectorControl(): FormControl {
    const control = this.form.get('connectorID');
    if (!control) {
      throw new Error('Connector control not found');
    }
    return control as FormControl;
  }

}

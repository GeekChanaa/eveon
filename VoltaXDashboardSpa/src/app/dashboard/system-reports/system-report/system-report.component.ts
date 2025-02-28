import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Route, Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { SystemReportComment } from 'src/_models/system-report-comment';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { SystemReportCommentService } from 'src/_services/system-report-comment.service';
import { SystemReportService } from 'src/_services/system-report.service';

@Component({
  selector: 'app-system-report',
  templateUrl: './system-report.component.html',
  styleUrls: ['./system-report.component.sass']
})
export class SystemReportComponent implements OnInit {

  systemReport : any = {};
  systemReportComments : SystemReportComment[] = [];
  isLoading : boolean = false;
  isLoadingComments : boolean = false;
  isLoadingCreatingComment : boolean = false;
  systemReportComment : any ={};
  uploadedImages: File[] = []; 

  constructor(
    private _systemReportService:  SystemReportService,
    private _route : ActivatedRoute,
    private _systemReportCommentService:  SystemReportCommentService,
    private _modalService : ActionModalService,
    private _authService : AuthService
  ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getSystemReport(id);
      this.getSystemReportComments(id);
      this.systemReportComment.userID = (this._authService.decodedToken.nameid)
    }
  }

  getSystemReport(id : number){
    this.isLoading = true;
    this._systemReportService.getSystemReportInformations(id).subscribe((data) => {
      this.isLoading = false;
      this.systemReport = data;
      this.systemReportComment.systemReportID = this.systemReport.id;
    })
  }

  getSystemReportComments(id : number){
    this.isLoadingComments = true;
    this._systemReportCommentService.getSystemReportComments(id).subscribe((data) => {
      this.isLoadingComments = false;
      if(data.result)
        this.systemReportComments = data.result;
    })
  }

  editSystemReport(id :number){
    this.isLoadingComments = true;
    this._systemReportService.getSystemReportInformations(id).subscribe((data) => {
      this.isLoadingComments = false;
      this.systemReport = data;
      this.systemReportComment.systemReportID = this.systemReport.id;
    })
  }

  deleteSystemReport(id : number){
    this._systemReportCommentService.deleteById(id).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"Delted","System Report "+id+" deleted sucessfully",4000);
    }, (error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Erro","Something Went wrong please contact your system administrator",4000);
    })
  }

  saveComment(){
    this.isLoadingCreatingComment = true;
    this._systemReportCommentService.create(this.systemReportComment).subscribe((data) => {
      this.isLoadingCreatingComment = false;
      this.getSystemReportComments(this.systemReportComment.systemReportID);
      this.resetCommentInput();
    }, (error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Erro","Something Went wrong please contact your system administrator",4000);
    })
  }

  resetCommentInput(){
    this.systemReportComment.content = "";
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer && event.dataTransfer.files) {
      const files = Array.from(event.dataTransfer.files);

      files.forEach((file) => {
        if (file.type.startsWith('image/')) {
          this.uploadedImages.push(file);

          this.systemReportComment.content += `\n[Image: ${file.name}]`;
        }
      });
    }
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      const files = Array.from(input.files);

      files.forEach((file) => {
        if (file.type.startsWith('image/')) {
          this.uploadedImages.push(file);
          this.systemReportComment.content += `\n[Image: ${file.name}]`;
        }
      });
    }
  }

}

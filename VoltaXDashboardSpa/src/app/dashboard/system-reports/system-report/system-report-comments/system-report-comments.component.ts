import { Component, Input, OnInit } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { SystemReportCommentService } from 'src/_services/system-report-comment.service';

@Component({
  selector: 'app-system-report-comments',
  templateUrl: './system-report-comments.component.html',
  styleUrls: ['./system-report-comments.component.sass']
})
export class SystemReportCommentsComponent implements OnInit {

  commentsLoaded = false;
  systemReportComments : any = [];
  systemReportComment : any = {};
  isLoadingCreatingComment : boolean = false;
  @Input() systemReportID : number = 0;
  uploadedImages: File[] = []; 

  constructor(
    private _systemReportCommentService : SystemReportCommentService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
    this.getSystemReportComments();
  }

  getSystemReportComments(){
    this._systemReportCommentService.getSystemReportComments(this.systemReportID).subscribe((data) => {
      this.commentsLoaded = true;
      if(data.result)
        this.systemReportComments = data.result;
    })
  }

  saveComment(){
    this.isLoadingCreatingComment = true;
    this._systemReportCommentService.create(this.systemReportComment).subscribe((data) => {
      this.isLoadingCreatingComment = false;
      this.getSystemReportComments();
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

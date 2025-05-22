import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { NoticeTypeEnum } from 'src/_models/_enums/notice-type-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { NoticeService } from 'src/_services/notice.service';

@Component({
  selector: 'app-create-notice',
  templateUrl: './create-notice.component.html',
  styleUrls: ['./create-notice.component.sass']
})
export class CreateNoticeComponent implements OnInit {

  cards : any[] = [];
    supports : any[]=[];
    chargePoints: any[] =[];
    isDragging = false;
    connectors : any[] =[];
    users : any[] = [];
    form: FormGroup;
    isLoading : boolean = false;
    selectedFile : File | null = null;
  
    constructor(
      private _noticeService: NoticeService,
      private _modalService: ActionModalService,
      private _router : Router
    ) { 
      this.form = new FormGroup({
        type : new FormControl("Maintenance", Validators.required),
        title : new FormControl("",Validators.required),
        isEmail : new FormControl(false),
        isSms : new FormControl(false),
        isPushNotification : new FormControl(false),
        forAdmins : new FormControl(false),
        forSupports : new FormControl(false),
        forPartners : new FormControl(false),
        forUsers : new FormControl(false),
        text : new FormControl("",Validators.required),
      });

      // Add validator to emailTemplate when isEmail is true
      this.form.get('isEmail')?.valueChanges.subscribe(value => {
        const emailTemplateControl = this.form.get('emailTemplate');
        if (value) {
          emailTemplateControl?.setValidators([Validators.required]);
        } else {
          emailTemplateControl?.clearValidators();
          emailTemplateControl?.setValue(null);
          this.selectedFile = null;
        }
        emailTemplateControl?.updateValueAndValidity();
      });
      
    }
  
    getFormControl(name: string): FormControl {
      return this.form.get(name) as FormControl;
    }
  
    ngOnInit() {
    }
  
   
  
    getControl(name: string): FormControl {
      return this.form.get(name) as FormControl;
    }

    onDragOver(event: DragEvent): void {
      event.preventDefault();
      event.stopPropagation();
      this.isDragging = true;
    }
  
    onDragLeave(event: DragEvent): void {
      event.preventDefault();
      event.stopPropagation();
      this.isDragging = false;
    }
  
    onFileDrop(event: DragEvent): void {
      event.preventDefault();
      event.stopPropagation();
      this.isDragging = false;
      
      const files = event.dataTransfer?.files;
      if ( files && files.length > 0) {
        this.handleFile(files[0]);
      }
    }
  
    onFileSelected(event: any): void {
      const files = event.target.files;
      if (files.length > 0) {
        this.handleFile(files[0]);
      }
    }
  
    handleFile(file: File): void {
      // Check if file is HTML
      if (file.type === 'text/html' || file.name.toLowerCase().endsWith('.html')) {
        this.selectedFile = file;
        this.form.get('emailTemplate')?.setValue(file);
      } else {
        this._modalService.popup(
          ActionModalStatusEnum.Error,
          "Invalid File Type",
          "Please upload only HTML files.",
          4000
        );
        this.form.get('emailTemplate')?.setValue(null);
      }
    }
  
    removeFile(): void {
      this.selectedFile = null;
      this.form.get('emailTemplate')?.setValue(null);
    }
  
    formatFileSize(bytes: number): string {
      if (bytes < 1024) {
        return bytes + ' bytes';
      } else if (bytes < 1048576) {
        return (bytes / 1024).toFixed(1) + ' KB';
      } else {
        return (bytes / 1048576).toFixed(1) + ' MB';
      }
    }
  
    onSubmit(): void {
      this.isLoading = true;
      
      // Create FormData to handle file upload
      const formData = new FormData();
      const formValue = this.form.value;
      
      // Append all form fields to FormData
      Object.keys(formValue).forEach(key => {
        if (key !== 'emailTemplate') {
          formData.append(key, formValue[key]);
        }
      });
      
      // Append file if exists
      if (this.selectedFile) {
        formData.append('emailTemplate', this.selectedFile);
      }
      
      this._noticeService.createNotice(formData).subscribe(
        (data) => {
          this.isLoading = false;
          this._modalService.popup(
            ActionModalStatusEnum.Success,
            "Success!",
            "Notice Created.",
            4000
          );
          this._router.navigateByUrl("/dashboard/notices");
        },
        (error) => {
          this.isLoading = false;
          this._modalService.popup(
            ActionModalStatusEnum.Error,
            "Error",
            "Something went wrong",
            4000
          );
        }
      );
    }

}

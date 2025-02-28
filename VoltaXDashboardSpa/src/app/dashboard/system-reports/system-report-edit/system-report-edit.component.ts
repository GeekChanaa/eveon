import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { SystemReportService } from 'src/_services/system-report.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-system-report-edit',
  templateUrl: './system-report-edit.component.html',
  styleUrls: ['./system-report-edit.component.sass']
})
export class SystemReportEditComponent implements OnInit {

  
    systemReportID : number = 0;
    systemReportLoaded : boolean = false;
    systemReportTypeValues : { [key: number]: string; } = {};
    updateSystemReportObservable = (id : number, model : any) => this._systemReportService.edit(id, model);
  
    systemReport: any = {};
  
    staticUrl : string = environment.apiStaticFilesUrl;
  
  
    constructor(
      private _systemReportService : SystemReportService,
      private _route: ActivatedRoute,
      private _enumService : EnumMappingService
    ) {
     }
  
    ngOnInit() {
      var idParam = this._route.snapshot.paramMap.get('id')
      if (idParam != null) {
        var id = parseInt(idParam);
        this.getSystemReportByID(id);
      }
    }
  
  
    getSystemReportByID(id : number){
      this.systemReportID = id;
      this._systemReportService.getSystemReportInformations(id).subscribe((cs) => {
        this.systemReport = cs;
        this.systemReportLoaded = true;
      })
    }


}

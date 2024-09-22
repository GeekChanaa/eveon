import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { ReportService } from 'src/_services/report.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-report',
  templateUrl: './report.component.html',
  styleUrls: ['./report.component.sass']
})
export class ReportComponent implements OnInit {


  reportID : number = 0;
  reportLoaded : boolean = false;
  CardTypesValues : any = {};
  CardStatusesValues : any = {};
  updateReportObservable = (id : number, model : any) => this._reportService.edit(id, model);

  report: any = {};

  staticUrl : string = environment.apiStaticFilesUrl;

  constructor(
    private _reportService: ReportService,
    private _route: ActivatedRoute,
    private _enumService : EnumMappingService
  ) {
   }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getReportByID(id);
    }
  }


  getReportByID(id : number){
    this.reportID = id;
    this._reportService.getReportByID(id).subscribe((cs) => {
      this.report = cs;
      this.reportLoaded = true;
    })
  }

}

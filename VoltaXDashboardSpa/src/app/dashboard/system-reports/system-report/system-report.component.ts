import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Route, Router } from '@angular/router';
import { SystemReportService } from 'src/_services/system-report.service';

@Component({
  selector: 'app-system-report',
  templateUrl: './system-report.component.html',
  styleUrls: ['./system-report.component.sass']
})
export class SystemReportComponent implements OnInit {

  systemReport : any = {};
  isLoading : boolean = false;

  constructor(
    private _systemReportService:  SystemReportService,
    private _route : ActivatedRoute
  ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getSystemReportService(id);
    }
  }

  getSystemReportService(id : number){
    this.isLoading = true;
    this._systemReportService.getSystemReportInformations(id).subscribe((data) => {
      this.isLoading = false;
      this.systemReport = data;
    })
  }

}

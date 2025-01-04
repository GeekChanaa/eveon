import { Component, OnInit } from '@angular/core';
import { ConfigurationService } from 'src/_services/configuration.service';

@Component({
  selector: 'app-global-configurations',
  templateUrl: './global-configurations.component.html',
  styleUrls: ['./global-configurations.component.sass']
})
export class GlobalConfigurationsComponent implements OnInit {

  configuration : any = {};

  constructor(
    private _configurationService: ConfigurationService
  ) { }

  ngOnInit() {
    this.getConfigurations();
  }

  getConfigurations(){
    this._configurationService.getGlobalConfigurations().subscribe((data) => {
      this.configuration = data;
    })
  }

}

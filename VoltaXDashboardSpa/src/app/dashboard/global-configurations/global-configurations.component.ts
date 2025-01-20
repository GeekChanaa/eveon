import { Component, OnInit } from '@angular/core';
import { GlobalConfigurations } from 'src/_models/global-configurations';
import { ConfigurationService } from 'src/_services/configuration.service';

@Component({
  selector: 'app-global-configurations',
  templateUrl: './global-configurations.component.html',
  styleUrls: ['./global-configurations.component.sass']
})
export class GlobalConfigurationsComponent implements OnInit {

  configuration? : GlobalConfigurations;

  constructor(
    private _configurationService: ConfigurationService
  ) { }

  ngOnInit() {
    this.getConfigurations();
  }

  getConfigurations(){
    this._configurationService.getGlobalConfigurations().subscribe((data : GlobalConfigurations) => {
      this.configuration = data;
    })
  }

}

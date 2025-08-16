import { Component, OnInit } from '@angular/core';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { GlobalConfigurations } from 'src/_models/global-configurations';
import { ConfigurationService } from 'src/_services/configuration.service';

@Component({
  selector: 'app-global-configurations',
  templateUrl: './global-configurations.component.html',
  styleUrls: ['./global-configurations.component.sass']
})
export class GlobalConfigurationsComponent implements OnInit {

  configuration? : GlobalConfigurations;
  PageState = PageState;
  state: PageState = PageState.Loading;

  constructor(
    private _configurationService: ConfigurationService
  ) { }

  ngOnInit() {
    this.getConfigurations();
  }

  getConfigurations(){
    this.state = PageState.Loading;
    this._configurationService.getGlobalConfigurations().subscribe((data : GlobalConfigurations) => {
      this.state = PageState.Success;
      this.configuration = data;
    })
  }

}

import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { BrowserModule } from '@angular/platform-browser';
import { AtomsModule } from '../atoms/atoms.module';
import { SharedModule } from '../shared/shared.module';
import { CommonModule } from '@angular/common';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';
import { GlobalRoutingModule } from './global-routing.module';
import { RechargeCardOrderComponent } from './recharge-card-order/recharge-card-order.component';
import { AboutUsComponent } from './about-us/about-us.component';
import { ContactUsComponent } from './contact-us/contact-us.component';
import { NotFoundComponent } from './error-pages/not-found/not-found.component';
import { NotAuthorizedComponent } from './error-pages/not-authorized/not-authorized.component';
import { ServerErrorComponent } from './error-pages/server-error/server-error.component';
import { PrivacyPolicyComponent } from './privacy-policy/privacy-policy.component';
import { TermsConditionsComponent } from './terms-conditions/terms-conditions.component';
import { IndexComponent } from './index/index.component';


@NgModule({
    declarations: [
      RechargeCardOrderComponent,
      AboutUsComponent,
      ContactUsComponent,
      NotFoundComponent,
      NotAuthorizedComponent,
      ServerErrorComponent,
      PrivacyPolicyComponent,
      TermsConditionsComponent,
      IndexComponent
    ],
    imports: [
        GlobalRoutingModule,
        AtomsModule,
        NgApexchartsModule,
        ReactiveFormsModule,
        CommonModule,
        SharedModule,
        FormsModule
    ],
  })
  export class GlobalModule { }
  
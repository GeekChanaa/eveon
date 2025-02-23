import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { DocumentationComponent } from './documentation.component';
import { CommonModule } from '@angular/common';
import { DocumentationRoutingModule } from './documentation-routing.module';
import { AtomsModule } from 'src/app/atoms/atoms.module';
import { RouterModule } from '@angular/router';
import { DocumentationComponentsComponent } from './documentation-components/documentation-components.component';
import { DocumentationVariablesComponent } from './documentation-variables/documentation-variables.component';
import { DocumentationMainComponent } from './documentation-main/documentation-main.component';
import { DocumentationComponentComponent } from './documentation-component/documentation-component.component';
import { DocumentationVariableComponent } from './documentation-variable/documentation-variable.component';
import { DocumentationIconsComponent } from './documentation-icons/documentation-icons.component';

@NgModule({
    declarations: [
      DocumentationComponent,
      DocumentationComponentsComponent,
      DocumentationVariablesComponent,
      DocumentationMainComponent,
      DocumentationComponentComponent,
      DocumentationVariableComponent,
      DocumentationIconsComponent
  ],
    imports: [
        DocumentationRoutingModule,
        AtomsModule,
        ReactiveFormsModule,
        CommonModule,
        FormsModule
    ],
  })
  export class DocumentationModule { }
  
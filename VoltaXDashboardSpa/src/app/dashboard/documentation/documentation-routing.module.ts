import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DocumentationMainComponent } from './documentation-main/documentation-main.component';
import { DocumentationComponentsComponent } from './documentation-components/documentation-components.component';
import { DocumentationVariablesComponent } from './documentation-variables/documentation-variables.component';
import { DocumentationVariableComponent } from './documentation-variable/documentation-variable.component';
import { DocumentationComponentComponent } from './documentation-component/documentation-component.component';
import { DocumentationIconsComponent } from './documentation-icons/documentation-icons.component';
const routes: Routes = [
  {
    path: "",
    component: DocumentationMainComponent
  },
  {
    path: "components",
    component: DocumentationComponentsComponent
  },
  {
    path: "icons",
    component: DocumentationIconsComponent
  },
  {
    path: "components/:name",
    component: DocumentationComponentComponent
  },
  {
    path: "variables",
    component: DocumentationVariablesComponent
  },
  {
    path: "variables/:name",
    component: DocumentationVariableComponent
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class DocumentationRoutingModule { }

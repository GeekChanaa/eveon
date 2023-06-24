import { Directive, TemplateRef } from "@angular/core";

@Directive({
    selector: '[appTableCustomButton]'
  })
  export class AppTableCustomButtonDirective {
    constructor(public template: TemplateRef<any>) { }
  }
  
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'camelCaseToSpace'
})
export class CamelCaseToSpacePipe implements PipeTransform {

  transform(value: string): string {
    if (!value) {
      return value;
    }
    // Add a space before each uppercase letter, then trim any leading/trailing spaces
    return value
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2')
      .trim();
  }
}

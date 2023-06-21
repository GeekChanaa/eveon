import { Pipe, PipeTransform } from '@angular/core';

@Pipe({name: 'camelCaseToSpace'})
export class CamelCaseToSpacePipe implements PipeTransform {
  transform(value: string): string {
    // Split the string on each uppercase letter and join it back with a space
    let result = value.replace(/([A-Z])/g, ' $1');

    // Capitalize the first letter and return the result
    return result.charAt(0).toUpperCase() + result.slice(1);
  }
}

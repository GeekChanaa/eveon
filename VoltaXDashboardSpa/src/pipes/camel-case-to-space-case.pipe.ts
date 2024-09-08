import { Pipe, PipeTransform } from '@angular/core';

@Pipe({name: 'camelCaseToSpace'})
export class CamelCaseToSpacePipe implements PipeTransform {
  transform(value: string): string {
    if (!value) return value;
    
    return value
      .replace(/([a-z])([A-Z])/g, '$1 $2')  
      .replace(/^./, (str) => str.toUpperCase()); 
  }
}

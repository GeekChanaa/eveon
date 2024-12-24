import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UtilsService {

constructor() { }

  generateRandomString() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    let result = [];
    
    for (let i = 0; i < 8; i++) {
        let segment = '';
        for (let j = 0; j < 8; j++) {
            segment += chars.charAt(Math.floor(Math.random() * chars.length));
        }
        result.push(segment);
    }
    
    return result.join('-');
  }

  generateRandomInteger() {
    return Math.floor(Math.random()*1000000);
  }

}

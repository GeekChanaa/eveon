import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ValidationMessagesService {
  private messages: any = {};

  constructor(private http: HttpClient) {}

  loadMessages(): Observable<any> {
    return this.http.get('/assets/data/validation-messages.json');
  }

  getMessage(errorKey: string, params?: any): string {
    let message = this.messages[errorKey] || 'Invalid input';
    
    if (params) {
      Object.keys(params).forEach(key => {
        message = message.replace(`{${key}}`, params[key]);
      });
    }

    return message;
  }

  setMessages(messages: any) {
    this.messages = messages;
  }
}

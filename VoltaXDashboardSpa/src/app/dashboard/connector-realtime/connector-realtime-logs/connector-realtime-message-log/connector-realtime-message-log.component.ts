import { Component, Input, OnInit } from '@angular/core';
import * as Prism from 'prismjs';
import 'prismjs/components/prism-json';

@Component({
  selector: 'app-connector-realtime-message-log',
  templateUrl: './connector-realtime-message-log.component.html',
  styleUrls: ['./connector-realtime-message-log.component.sass']
})
export class ConnectorRealtimeMessageLogComponent implements OnInit {

  @Input() messageLog : any = {};
  
  highlightedSentContent : any = {};
  highlightedReceivedContent : any = {};

  constructor() { }

  ngOnInit() {
    const formattedJsonSentContent = JSON.stringify(JSON.parse(this.messageLog.contentSent.trim()), null, 2); // 2 spaces for indentation
    this.highlightedSentContent = Prism.highlight(formattedJsonSentContent, Prism.languages['json'], 'json');
    const formattedJsonReceivedContent = JSON.stringify(JSON.parse(this.messageLog.contentReceived.trim()), null, 2); // 2 spaces for indentation
    this.highlightedReceivedContent = Prism.highlight(formattedJsonReceivedContent, Prism.languages['json'], 'json');
  }


}

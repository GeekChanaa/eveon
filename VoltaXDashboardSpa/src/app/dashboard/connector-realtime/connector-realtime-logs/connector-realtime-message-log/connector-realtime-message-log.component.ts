import { Component, EventEmitter, HostListener, Input, OnInit, Output } from '@angular/core';
import * as Prism from 'prismjs';
import 'prismjs/components/prism-json';

@Component({
  selector: 'app-connector-realtime-message-log',
  templateUrl: './connector-realtime-message-log.component.html',
  styleUrls: ['./connector-realtime-message-log.component.sass']
})
export class ConnectorRealtimeMessageLogComponent implements OnInit {

  @Input() messageLog : any = {};
  @Output() closed = new EventEmitter<void>();

  highlightedSentContent : string | null = null;
  highlightedReceivedContent : string | null = null;

  constructor() { }

  ngOnInit() {
    this.highlightedSentContent = this.highlight(this.messageLog.contentSent);
    this.highlightedReceivedContent = this.highlight(this.messageLog.contentReceived);
  }

  @HostListener('document:keydown.escape')
  close(){
    this.closed.emit();
  }

  /**
   * Pretty-prints the payload when it is JSON. Some logs carry no payload, or a raw string
   * that is not JSON — those are shown as-is instead of throwing and blanking the modal.
   */
  private highlight(content : string | null | undefined) : string | null {
    if(content == null || content.trim() == "")
      return null;

    let formatted = content.trim();
    try {
      formatted = JSON.stringify(JSON.parse(formatted), null, 2);
    } catch {
      // not JSON, keep the raw text
    }
    return Prism.highlight(formatted, Prism.languages['json'], 'json');
  }

}

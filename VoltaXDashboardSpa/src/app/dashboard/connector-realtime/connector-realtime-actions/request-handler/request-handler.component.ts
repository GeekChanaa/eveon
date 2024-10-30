import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { RequestCallerService } from 'src/_services/ocpp-services/request-caller.service';
import { SchemaService } from 'src/_services/ocpp-services/schema.service';
import * as Prism from 'prismjs';
import 'prismjs/components/prism-json';  // Load the language component

@Component({
  selector: 'app-request-handler',
  templateUrl: './request-handler.component.html',
  styleUrls: ['./request-handler.component.sass']
})
export class RequestHandlerComponent implements OnInit {

  @Input() ocppAction : any = {};
  @Input() chargePointID : string = "";
  @Output() closeEvent : EventEmitter<void> = new EventEmitter();

  jsonOverload : any = {};
  highlightedJson : any = {};

  constructor(
    private _schemaService : SchemaService,
    private _requestCallerService : RequestCallerService
  ) { }

  ngOnInit() {
    
  }

  closeModal(){
    this.closeEvent.emit();
  }

  generate(){
    this._schemaService.generateRandomObject(this.ocppAction.schemaPath).subscribe((data) => {
      this.jsonOverload = JSON.stringify(data,null,2);
    
      this.highlightedJson = Prism.highlight(this.jsonOverload.trim(), Prism.languages['json'], 'json');
    })
  }

  onTextChange(event: Event): void {
    const target = event.target as HTMLTextAreaElement;
    this.jsonOverload = target.value;

    this.highlightJson();
  }

  highlightJson(): void {
    try {
      const parsedJson = JSON.parse(this.jsonOverload.trim(""));
      const prettyJsonString = JSON.stringify(parsedJson, null, 2);
      this.highlightedJson = Prism.highlight(prettyJsonString, Prism.languages['json'], 'json');
    } catch (error) {
      this.highlightedJson = Prism.highlight(this.jsonOverload.trim(), Prism.languages['json'], 'json');
    }
    this.highlightedJson = this.removeLeadingSpaces(this.highlightedJson);
  }

  removeLeadingSpaces(highlightedString: string): string {
    return highlightedString.replace(/^\s+/, '');
  }

  sendRequest(){
    console.log("this is the request we're sending : ");
    console.log(this.jsonOverload);
    this._requestCallerService.sendOCPPMessage(this.chargePointID,this.ocppAction.name,this.jsonOverload)?.subscribe((data) => {
      console.log("request sent");
      console.log(data);
    });
  }

}

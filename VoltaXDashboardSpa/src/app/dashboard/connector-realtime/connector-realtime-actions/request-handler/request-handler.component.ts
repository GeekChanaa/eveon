import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { SchemaService } from 'src/_services/ocpp-services/schema.service';

@Component({
  selector: 'app-request-handler',
  templateUrl: './request-handler.component.html',
  styleUrls: ['./request-handler.component.sass']
})
export class RequestHandlerComponent implements OnInit {

  @Input() ocppAction : any = {};
  @Output() closeEvent : EventEmitter<void> = new EventEmitter();

  constructor(
    private _schemaService : SchemaService
  ) { }

  ngOnInit() {
    console.log("this is the current ocpp Action");
    console.log(this.ocppAction);
  }

  closeModal(){
    this.closeEvent.emit();
  }

  generate(){
    this._schemaService.generateRandomObject(this.ocppAction.schemaPath).subscribe((data) => {
      console.log("this is the example object : ");
      console.log(data);
    })
  }

}

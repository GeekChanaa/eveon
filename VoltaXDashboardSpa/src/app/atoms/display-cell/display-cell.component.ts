import { AfterViewInit, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

@Component({
  selector: 'app-display-cell',
  templateUrl: './display-cell.component.html',
  styleUrls: ['./display-cell.component.css']
})
export class DisplayCellComponent implements OnInit, AfterViewInit {

  @Input() title : any = {};
  @Input() val : any = {};
  @Input() inpType : string = "text";
  @Input() enumName : string = "";
  @Output() updatePropertyEvent : EventEmitter<any> = new EventEmitter<any>();

  enumMappings: { [key: string]: { [id: number]: string } } = {}
  updatedValue : any = {};
  editing : boolean = false;

  // constructor
  constructor(
    private _enumService : EnumMappingService
  ) { }

  ngOnInit() {
    this.updatedValue = this.val;
    console.log(this.val);  
    if(this.inpType == 'select_enum'){
      this.enumMappings = this._enumService.getEnumMapping(this.enumName);
    }
  }

  getEnumKeys() {
    return Object.keys(this.enumMappings);
  }

  update(){
    if(this.inpType == "text")
    this.updatePropertyEvent.emit(this.updatedValue);
    if(this.inpType == "select_enum")
    this.updatePropertyEvent.emit(parseInt(this.updatedValue));
    this.editing = false;
  }

  ngAfterViewInit(){
  }

}

import { AfterViewInit, Component, ElementRef, EventEmitter, Input, OnInit, Output, Renderer2 } from '@angular/core';
import { Observable } from 'rxjs';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

@Component({
  selector: 'app-display-cell',
  templateUrl: './display-cell.component.html',
  styleUrls: ['./display-cell.component.sass']
})
export class DisplayCellComponent implements OnInit, AfterViewInit {

  @Input() title : any = {};
  @Input() val : any = {};
  @Input() object : any = {};
  @Input() inpType : string = "text";
  @Input() enumName : string = "";
  @Input() editable : boolean = true;
  @Input() isLink : boolean = false;
  @Input() link : string = "";

  isLoading : boolean = false;

  @Input() updateObservable! : (id : number, object :any) => Observable<any>;

  enumMappings: { [key: string]: { [id: number]: string } } = {}
  updatedValue : any = {};
  editing : boolean = false;

  // constructor
  constructor(
    private _enumService : EnumMappingService,
    private _modalService : ActionModalService,
    private _elRef: ElementRef, 
    private _renderer: Renderer2
  ) { }

  ngOnInit() {
    this.updatedValue = this.val;
    console.log(this.val);
    if(this.inpType == 'select_enum'){
      this.enumMappings = this._enumService.getEnumMapping(this.enumName);
      console.log("this is the enumMappings");
      console.log(this.enumMappings);
    }
  }

  onEnterPress(event: KeyboardEvent) {
    const activeElement = document.activeElement as HTMLElement;

    if (activeElement === this._elRef.nativeElement.querySelector('input')) {
      this.updateVal()
    } 
  }

  getEnumKeys() {
    return Object.keys(this.enumMappings);
  }

  getEnumValues() {
    return Object.values(this.enumMappings);
  }

  ngAfterViewInit(){
  }

  updateVal(){
    this.object[this.title] = this.updatedValue;
    this.isLoading = true;
    this.updateObservable(this.object.id, this.object).subscribe((data) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Succes !", "Updated successfully", 4000);
      this.editing = false;
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !", "Something went wrong please try again later.", 4000);
    })
  }

}

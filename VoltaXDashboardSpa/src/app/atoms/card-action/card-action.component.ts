import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-card-action',
  templateUrl: './card-action.component.html',
  styleUrls: ['./card-action.component.sass']
})
export class CardActionComponent implements OnInit {

  @Input() actionName : string = ""
  @Input() icon : string = "info"
  @Output() actionClick = new EventEmitter<void>();
  
  constructor() { }

  ngOnInit() {
  }

  action(){
    this.actionClick.emit();
  }

}

import { Component, OnInit } from '@angular/core';
import { ICONS_LIST } from 'src/assets/data/icons-list';

@Component({
  selector: 'app-documentation-icons',
  templateUrl: './documentation-icons.component.html',
  styleUrls: ['./documentation-icons.component.sass']
})
export class DocumentationIconsComponent implements OnInit {

  iconList : string[] = ICONS_LIST;
  copiedIcon = '';
  private copyNoticeTimeout?: ReturnType<typeof setTimeout>;

  constructor(
  ) { }

  ngOnInit() {
  }

  copy(icon : string){
    navigator.clipboard.writeText(icon).then(() => {
      this.copiedIcon = icon;
      clearTimeout(this.copyNoticeTimeout);
      this.copyNoticeTimeout = setTimeout(() => this.copiedIcon = '', 2200);
    });
  }

}

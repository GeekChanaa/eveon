import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  templateUrl: './pagination.component.html',
  styleUrls: ['./pagination.component.sass']
})
export class PaginationComponent implements OnInit {

  @Input() currentPage : number = 1;
  paginationPages : any[] = [];
  @Input() pagination : any = {};
  @Output() refreshEvent : EventEmitter<number> = new EventEmitter<number>();

  constructor() { }

  ngOnInit() {
    this.currentPage = this.pagination.currentPage;
    console.log("this is the paginatio for this component");
    console.log(this.pagination);
    this.generatePaginationLinks();
  }

  generatePaginationLinks() {
    const currentPage = this.pagination.currentPage;
    const totalPages = this.pagination.totalPages;
    if (totalPages <= 3) {
      this.paginationPages = Array.from({ length: totalPages }, (_, i) => i + 1);
    } else if (currentPage === 1) {
      this.paginationPages = [1, 2, '...', totalPages];
    } else if (currentPage == totalPages) {
      this.paginationPages = [currentPage - 2, currentPage - 1, '...', totalPages];
    } else {
      this.paginationPages = [currentPage - 1, currentPage, '...', totalPages];
    }
    
  }

  nextPage() {
    this.currentPage++;
    console.log("this is the current page right now : " + this.currentPage);
    this.refreshEvent.emit(this.currentPage);
  }

  // Previous Page
  previousPage() {
    this.currentPage--;
    console.log("this is the current page right now : " + this.currentPage);
    this.refreshEvent.emit(this.currentPage);
  }

  goToPage(page : number){
    this.currentPage = page;
    this.refreshEvent.emit(this.currentPage);
  }


}

import { Component, OnInit } from '@angular/core';
import { Comment } from 'src/_models/comment';
import { CommentService } from 'src/_services/comment.service';

@Component({
  selector: 'app-comment-management',
  templateUrl: './comment-management.component.html',
  styleUrls: ['./comment-management.component.css']
})
export class CommentManagementComponent implements OnInit {

  
  // Data
  data : any[] = [];

  // Fields
  fields : string[] = [];

  // Page params
  itemsPerPage : number = 20;
  currentPage : number = 1;

  comment : Comment = {
    id: 0,
    UserID: 0,
    Rating: 0,
    Text: '',
    ChargingStationID: 0,
    PointID: 0,
    CommentTime: new Date(),
    User: null,
    ChargingStation: null
  }

  // Constructor
  constructor(
    private _commentService : CommentService
  ) { }

  ngOnInit() {
    this._getItemFields();
    this.getAll();
  }

  
  // Getting All Products
  getAll(){
    this._commentService.getAll(this.currentPage,this.itemsPerPage).subscribe(data => {
      if (data.result) {
        this.data = data.result;
      }
    })
  }

  // Getting Item Fields
  private _getItemFields(){
    // Ensure this.chargingStation is defined
    if (!this.comment || this.comment == undefined) {
      return;
    }
    // Getting item fields
    Object.keys(this.comment ?? {}).forEach((element : string) => {
      console.log(element); 
      if(typeof this.comment?.[element] == "object" && this.comment?.[element] != null && this.comment?.[element].constructor.name == "Date")
      this.fields.push(element);
      if(typeof this.comment?.[element] != "object") this.fields.push(element);
    });
  }

  // Deleting the item

  // Next page
  nextPage(){
    this.currentPage++;
    this.getAll();
  }

  // Previous Page
  previousPage(){
    this.currentPage--;
    this.getAll();
  }

}

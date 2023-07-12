import { Component, OnInit } from '@angular/core';
import * as html2pdf from 'html2pdf.js';
import { InvoiceService } from 'src/_services/invoice.service';
import { Subscription } from 'rxjs';
@Component({
  selector: 'app-invoice',
  templateUrl: './invoice.component.html',
  styleUrls: ['./invoice.component.css']
})
export class InvoiceComponent implements OnInit {

  private downloadSubscription!: Subscription;

  constructor(private invoiceService: InvoiceService) { }

  ngOnInit() {
    
  }

  

}

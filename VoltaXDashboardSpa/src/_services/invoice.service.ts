import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import * as html2pdf from 'html2pdf.js';
import { InvoiceDTO } from 'src/_models/_dtos/invoice-dto';

@Injectable({
  providedIn: 'root',
})
export class InvoiceService {
  constructor(private sanitizer: DomSanitizer) {}

  private downloadSubject = new Subject<void>();

  private generateHtmlString(item : InvoiceDTO): SafeHtml {
    // Create the HTML string with inline styles
    let htmlString = `
<div style="width: 80%; margin-left: 10%;" id="invoice">
  <div style="display: flex;">
    <div style="height: 90px;">
      <img src="dd" alt="">
    </div>
    <div style="margin-left: auto; font-size: 3em; font-weight: lighter;">
      INVOICE #1000
    </div>    
  </div>
  <div style="display: flex;">   
    <table style="margin-top: 64px; width: 45%;">
      <tr>
        <td style="font-size: 1.5em; font-weight: bold;">Billed to :</td>
        <td style="font-size: 1.2em; font-weight: lighter;">mohammed chanaa</td>
      </tr>
      <!-- add more rows as needed -->
    </table>   
  </div>
  <div>
    <p style="margin-top: 64px; margin-bottom: 32px; font-size: 2em;">Details</p>
    <div>
      <table style="text-align: center;">
        <thead style="font-size: 2em;">
          <th>Description</th>
          <th>Amount</th>
        </thead>
        <tbody style="font-size: 2em; font-weight: lighter;">
          <tr>
            <td>fmlk</td>
            <td>msldkf</td>
          </tr>
        </tbody>     
      </table>
    </div>
  </div>
  <div>
    <p style="font-size: 1.5em; margin-top: 64px;">Thank you for your purchase ! </p>
    <p>For any further questions please contact us at support@voltax.com </p>
  </div>
</div>
`;

    // Bypass security trust HTML
    let safeHtmlString = this.sanitizer.bypassSecurityTrustHtml(htmlString);

    return safeHtmlString;
}


  private invoiceData: any; // replace 'any' with the type of your invoice data

  setInvoiceData(invoiceData: any) {
    this.invoiceData = invoiceData;
  }

  downloadPdf(item : InvoiceDTO) {
    try {
      let safeHtmlString = this.generateHtmlString(item);

      // Convert the HTML string to a DOM element
      let template = document.createElement('template');
      template.innerHTML = safeHtmlString as unknown as string; // Cast SafeHtml to string
      let domElement = template.content.firstElementChild;

      let opt = {
        margin: [1, 0.5, 1, 0.5],
        filename: 'Invoice.pdf',
        image: { type: 'jpeg', quality: 0.98 },
        html2canvas: { scale: 2 },
        jsPDF: { unit: 'in', format: 'letter', orientation: 'portrait' },
      };

      console.log('About to download the pdf');
      console.log(domElement);
      // Generate and download the PDF
      html2pdf()
        .set(opt)
        .from(domElement)
        .save()
        .then(() => console.log('PDF Downloaded'))
        .catch((error: any) =>
          console.error('Error occurred while trying to download PDF: ', error)
        );
    } catch (error: any) {
      console.error('Error occurred: ', error);
    }
  }
}

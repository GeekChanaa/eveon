import { Injectable } from '@angular/core';
import * as html2pdf from 'html2pdf.js';
import { InvoiceDTO } from 'src/_models/_dtos/invoice-dto';

// Every value interpolated into the invoice template must go through this.
export function escapeHtml(value: unknown): string {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

@Injectable({
  providedIn: 'root',
})
export class InvoiceService {

  private generateHtmlString(invoiceData : InvoiceDTO): string {
    const e = escapeHtml;
    return `
<div style="width: 80%; margin-left: 10%;" id="invoice">
  <div style="display: flex;">
    <div style="margin-left: auto; font-size: 3em; font-weight: lighter;">
      INVOICE #${e(invoiceData.orderNumber)}
    </div>
  </div>
  <div style="display: flex;">
    <table style="margin-top: 64px; width: 65%;">
      <tr>
        <td style="font-size: 1.2em; font-weight: bold;">Billed to :</td>
        <td style="font-size: 0.8em; font-weight: lighter;">${e(invoiceData.billedTo)}</td>
      </tr>
      <tr>
        <td style="font-size: 1.2em; font-weight: bold;">Pay to :</td>
        <td style="font-size: 0.8em; font-weight: lighter;">${e(invoiceData.payTo)}</td>
      </tr>
      <tr>
        <td style="font-size: 1.2em; font-weight: bold;">Payment Method :</td>
        <td style="font-size: 0.8em; font-weight: lighter;">${e(invoiceData.paymentMethod)}</td>
      </tr>
      <tr>
        <td style="font-size: 1.2em; font-weight: bold;">Phone Number :</td>
        <td style="font-size: 0.8em; font-weight: lighter;">${e(invoiceData.phone)}</td>
      </tr>
      <tr>
        <td style="font-size: 1.2em; font-weight: bold;">Email :</td>
        <td style="font-size: 0.8em; font-weight: lighter;">${e(invoiceData.email)}</td>
      </tr>
      <tr>
        <td style="font-size: 1.2em; font-weight: bold;">Date :</td>
        <td style="font-size: 0.8em; font-weight: lighter;">${e(invoiceData.date)}</td>
      </tr>
    </table>
  </div>
  <div>
    <p style="font-size: 1.2em; margin-top: 64px;">Thank you for your purchase ! </p>
    <p>For any further questions please contact our support team.</p>
  </div>
</div>
`;
  }

  private invoiceData: any;

  setInvoiceData(invoiceData: any) {
    this.invoiceData = invoiceData;
  }

  downloadPdf(item : InvoiceDTO) {
    try {
      const template = document.createElement('template');
      template.innerHTML = this.generateHtmlString(item);
      const domElement = template.content.firstElementChild;

      const opt = {
        margin: [1, 0.5, 1, 0.5],
        filename: 'Invoice.pdf',
        image: { type: 'jpeg', quality: 0.98 },
        html2canvas: { scale: 2 },
        jsPDF: { unit: 'in', format: 'letter', orientation: 'portrait' },
      };

      html2pdf()
        .set(opt)
        .from(domElement)
        .save()
        .catch((error: any) =>
          console.error('Error occurred while trying to download PDF: ', error)
        );
    } catch (error: any) {
      console.error('Error occurred: ', error);
    }
  }
}

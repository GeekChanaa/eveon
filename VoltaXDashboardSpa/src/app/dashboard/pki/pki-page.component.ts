import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AtomsModule } from '../../atoms/atoms.module';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ChargerCaInfo, ChargerPkiService } from 'src/_services/charger-pki.service';

@Component({
  selector: 'app-pki-page',
  standalone: true,
  imports: [CommonModule, AtomsModule],
  templateUrl: './pki-page.component.html',
  styleUrls: ['./pki-page.component.sass']
})
export class PkiPageComponent implements OnInit {
  state = PageState.Loading;
  ca: ChargerCaInfo | null = null;
  downloading = false;
  error = '';

  constructor(private pki: ChargerPkiService) {}

  ngOnInit(): void {
    this.pki.ca().subscribe({
      next: ca => { this.ca = ca; this.state = PageState.Success; },
      error: () => this.state = PageState.Error
    });
  }

  get daysLeft(): number | null {
    return this.ca?.notAfter ? Math.floor((new Date(this.ca.notAfter).getTime() - Date.now()) / 86400000) : null;
  }

  download(): void {
    this.downloading = true;
    this.error = '';
    this.pki.caCertificate().subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'eveon-charger-ca.pem';
        link.click();
        URL.revokeObjectURL(url);
        this.downloading = false;
      },
      error: () => { this.downloading = false; this.error = 'Could not download the CA certificate.'; }
    });
  }
}

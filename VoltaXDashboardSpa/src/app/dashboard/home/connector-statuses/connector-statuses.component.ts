import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { ConnectorStatusService } from 'src/_services/connector-status.service';
import { ConnectorStatusesDto } from 'src/_models/_dtos/connector-statuses-dto';
@Component({
  selector: 'app-connector-statuses',
  templateUrl: './connector-statuses.component.html',
  styleUrls: ['./connector-statuses.component.sass']
})
export class ConnectorStatusesComponent implements OnInit, OnDestroy {
  loading = false;
  error = false;
  updatedAt: Date | null = null;
  private request?: Subscription;
  statuses = [
    { key: 'nbrAvailableConnectors', label: 'Available', detail: 'Ready for a new driver', tone: 'available', count: null as number | null },
    { key: 'nbrOccupiedConnectors', label: 'Occupied', detail: 'Currently in use', tone: 'occupied', count: null as number | null },
    { key: 'nbrReservedConnectors', label: 'Reserved', detail: 'Held for a driver', tone: 'reserved', count: null as number | null },
    { key: 'nbrUnavailableConnectors', label: 'Unavailable', detail: 'Out of service', tone: 'unavailable', count: null as number | null },
    { key: 'nbrFaultedConnectors', label: 'Faulted', detail: 'Reported a connector error', tone: 'faulted', count: null as number | null },
    { key: 'nbrDisconnectedConnectors', label: 'Disconnected', detail: 'Not connected to the system', tone: 'disconnected', count: null as number | null }
  ];
  constructor(private service: ConnectorStatusService) {}
  ngOnInit() { this.getConnectorStatusNumbers(); }
  ngOnDestroy() { this.request?.unsubscribe(); }
  getConnectorStatusNumbers() {
    if (this.loading) return;
    this.loading = true;
    this.error = false;
    this.request = this.service.getNumberOfConnectorsByAllStatus().subscribe({
      next: data => {
        this.statuses.forEach(status => {
          const value = data?.[status.key as keyof ConnectorStatusesDto];
          status.count = typeof value === 'number' ? value : null;
        });
        this.updatedAt = new Date();
        this.loading = false;
      },
      error: () => { this.loading = false; this.error = true; }
    });
  }
}

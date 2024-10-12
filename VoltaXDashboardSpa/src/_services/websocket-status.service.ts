import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject } from 'rxjs';
import { interval, Subscription } from 'rxjs';
import { environment } from 'src/environments/environment';


@Injectable({
  providedIn: 'root'
})
export class WebSocketStatusService implements OnDestroy {
  private pollingInterval = 5000; 
  private pollingSubscription: Subscription | undefined;
  public connectionStatus$ = new Subject<{ chargePointID: string; isActive: boolean }>();

  constructor(private http: HttpClient) {
    this.startPolling();
  }

  private startPolling() {
    this.pollingSubscription = interval(this.pollingInterval).subscribe(() => {
      this.checkWebSocketStatus('your-connection-id');
    });
  }

  private checkWebSocketStatus(chargePointID: string) {
    this.http.get<{ chargePointID: string; isActive: boolean }>(`${environment.apiUrl}/api/chargePointRealTime/status/${chargePointID}`)
      .subscribe(
        response => this.connectionStatus$.next(response),
        error => console.error('Error fetching WebSocket status:', error)
      );
  }

  ngOnDestroy() {
    this.pollingSubscription?.unsubscribe();
  }
}

import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Subject } from 'rxjs';
import { interval, Subscription } from 'rxjs';
import { environment } from 'src/environments/environment';


@Injectable({
  providedIn: 'root'
})
export class WebSocketStatusService implements OnDestroy {
  private pollingInterval = 5000; 
  private pollingSubscription: Subscription | undefined;
  private connectionStatusSubject = new BehaviorSubject<any | null>(null);
  public connectionStatus$ = this.connectionStatusSubject.asObservable();

  constructor(private http: HttpClient) {}

  public startPolling(chargePointID: string) {
    this.pollingSubscription?.unsubscribe(); // Unsubscribe from any previous polling
    this.pollingSubscription = interval(this.pollingInterval).subscribe(() => {
      this.checkWebSocketStatus(chargePointID);
    });
  }

  private checkWebSocketStatus(chargePointID: string) {
    this.http.get<{ isActive: boolean }>(`${environment.apiUrl}/api/chargePointRealTime/status/${chargePointID}`)
      .subscribe(
        response => {
          const status: any = { chargePointID, isActive: response.isActive };
          this.connectionStatusSubject.next(status);
        },
        error => console.error('Error fetching WebSocket status:', error)
      );
  }

  ngOnDestroy() {
    this.pollingSubscription?.unsubscribe();
  }
}

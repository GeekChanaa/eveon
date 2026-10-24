import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, defer, distinctUntilChanged, filter, map, merge, of, shareReplay, switchMap, timer } from 'rxjs';
import { environment } from 'src/environments/environment';
import { RealtimeHubService, chargerGroup } from './realtime-hub.service';

/** Slow safety check behind the pushed status (a push missed during a deploy, another instance...). */
export const CONNECTION_FALLBACK_POLL_MS = 60000;

/**
 * Whether a charge point is connected to the CSMS, per charge point: read once, then pushed by the API on every
 * connection / disconnection (charger hub, "ChargerConnection"), re-read after a hub reconnection and every minute as
 * a fallback. Nothing runs while nobody subscribes: subscribe with the page's lifetime (async pipe, takeUntilDestroyed).
 */
@Injectable({ providedIn: 'root' })
export class ChargerConnectionService {
  private readonly streams = new Map<string, Observable<boolean | null>>();

  constructor(private http: HttpClient, private hub: RealtimeHubService) {}

  /** true: connected, false: disconnected, null: unknown (status could not be read). */
  status(chargePointId: string): Observable<boolean | null> {
    let stream = this.streams.get(chargePointId);
    if (!stream) {
      const read = defer(() => this.http.get<{ isActive: boolean }>(`${environment.apiUrl}/api/chargePointRealTime/status/${encodeURIComponent(chargePointId)}`))
        .pipe(map(r => !!r?.isActive), catchError(() => of(null)));
      stream = merge(
        timer(0, CONNECTION_FALLBACK_POLL_MS).pipe(switchMap(() => read)),
        this.hub.reconnected('charger').pipe(switchMap(() => read)),
        this.hub.on<{ chargePointId: string; isActive: boolean }>('charger', 'ChargerConnection', chargerGroup(chargePointId))
          // One connection carries the groups of every watched charger.
          .pipe(filter(event => event?.chargePointId === chargePointId), map(event => !!event.isActive))
      ).pipe(
        distinctUntilChanged(),
        shareReplay({ bufferSize: 1, refCount: true })
      );
      this.streams.set(chargePointId, stream);
    }
    return stream;
  }
}

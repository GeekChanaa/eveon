import { AccessService } from 'src/_services/access.service';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargerConnectionService } from 'src/_services/charger-connection.service';

export enum ChargerNavbarEnum{
  Overview = 1,
  PricingAccess = 2,
  DetailsSettings = 3,
  Actions = 4,
  OCPPConfiguration = 5,
  Firmware = 6,
  UptimeReports = 7,
  Logs = 8,
  Ratings = 9,
  ChargingSessions = 10,
  Transactions = 11,
  Configurations = 12
}

@Component({
  selector: 'app-connector-realtime-main',
  templateUrl: './connector-realtime-main.component.html',
  styleUrls: ['./connector-realtime-main.component.sass']
})
export class ConnectorRealtimeMainComponent implements OnInit {

  selectedMenuItem : ChargerNavbarEnum = ChargerNavbarEnum.Overview;

  /** inactive until the first status is known, then available (connected) or disconnected. */
  status : string = "inactive";

  /** Commands (the Actions tab) can only be sent while the charger is connected. */
  online : boolean = false;

  chargePoint : any = {};
  chargePointID : number = 0
  loading : boolean = false;
  loadError : boolean = false;

  private readonly destroyRef = inject(DestroyRef);

  selectMenuItem(item: ChargerNavbarEnum): void {
    if (this.canOpenMenu(item) && (item !== ChargerNavbarEnum.Actions || this.online)) this.selectedMenuItem = item;
  }

  canOpenMenu(item: number): boolean {
    const permission: Record<number, string> = { 4: 'OperateChargePoints', 5: 'ViewChargePointConfiguration', 6: 'OperateChargePoints', 8: 'ViewChargePointConfiguration', 10: 'ViewChargingSessions', 11: 'ViewTransactions', 12: 'ViewChargePointConfiguration' };
    return this.access.can(permission[item] || 'ViewChargePoints');
  }

  constructor(
    public access: AccessService,
    private _route: ActivatedRoute,
    private _chargePointService: ChargePointService,
    private _connection: ChargerConnectionService
    ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getChargePointByID(id);
      this.chargePointID = id
    }
    
  }

  getChargePointByID(id : number){
    this.loading = true;
    this.loadError = false;
    this._chargePointService.getChargePointByID(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (cp) => {
        this.loading = false;
        this.chargePoint = cp;
        this.watchConnection(cp.chargePointId);
      },
      error: () => { this.loading = false; this.loadError = true; }
    });
  }

  /** Connection status pushed by the API while this page is open (read at once, then on every change). */
  private watchConnection(chargePointId: string) {
    this._connection.status(chargePointId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(isActive => {
      this.status = isActive == null ? 'inactive' : isActive ? 'available' : 'disconnected';
      this.online = isActive === true;
      // Commands cannot reach a charger that went offline: leave the Actions tab.
      if (!this.online && this.selectedMenuItem === ChargerNavbarEnum.Actions) this.selectedMenuItem = ChargerNavbarEnum.Overview;
    });
  }

  getMenu(stepName: string): ChargerNavbarEnum | undefined {
    return ChargerNavbarEnum[stepName as keyof typeof ChargerNavbarEnum];
  }


}

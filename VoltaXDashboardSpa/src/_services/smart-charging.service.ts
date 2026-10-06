import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export type ChargingProfilePurpose = 'ChargingStationMaxProfile' | 'TxDefaultProfile' | 'TxProfile' | 'ChargingStationExternalConstraints';
export type ChargingProfileKind = 'Absolute' | 'Recurring' | 'Relative';
export type ChargingProfileRecurrency = 'Daily' | 'Weekly';
export type ChargingRateUnit = 'A' | 'W';
export type ChargingProfileSource = 'Csms' | 'LoadBalancer' | 'Strategy' | 'ChargerReported';
export type ChargingProfileStatus = 'Pending' | 'Accepted' | 'Rejected' | 'Cleared';
export type LoadBalancingStrategy = 'EqualShare' | 'FirstComeFirstServed';

export interface ChargingProfilePeriod {
  startPeriod: number;
  limit: number;
  numberPhases?: number | null;
  phaseToUse?: number | null;
}

export interface SmartChargePoint {
  id: number;
  chargePointId: string;
  chargingStationID: number;
  chargingStationName: string;
  evseIds: number[];
  /** "ocpp2.0.1", "ocpp1.6" or null when offline. */
  protocol: string | null;
}

export interface ChargingProfile {
  id: number;
  chargePointID: number;
  evseId: number;
  ocppProfileId: number;
  stackLevel: number;
  purpose: ChargingProfilePurpose;
  kind: ChargingProfileKind;
  recurrencyKind?: ChargingProfileRecurrency | null;
  validFrom?: string | null;
  validTo?: string | null;
  transactionId?: string | null;
  chargingRateUnit: ChargingRateUnit;
  startSchedule?: string | null;
  duration?: number | null;
  minChargingRate?: number | null;
  periods: ChargingProfilePeriod[];
  source: ChargingProfileSource;
  status: ChargingProfileStatus;
  lastError?: string | null;
  chargingLimitSource?: string | null;
  chargingStrategyID?: number | null;
  lastSentAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ChargingProfileInput {
  chargingProfileID?: number | null;
  evseId: number;
  stackLevel: number;
  purpose: ChargingProfilePurpose;
  kind: ChargingProfileKind;
  recurrencyKind?: ChargingProfileRecurrency | null;
  validFrom?: string | null;
  validTo?: string | null;
  startSchedule?: string | null;
  duration?: number | null;
  chargingRateUnit: ChargingRateUnit;
  minChargingRate?: number | null;
  transactionId?: string | null;
  periods: ChargingProfilePeriod[];
}

export interface CompositeSchedule {
  status: string;
  evseId: number;
  scheduleStart?: string | null;
  duration?: number | null;
  chargingRateUnit?: ChargingRateUnit | null;
  periods: ChargingProfilePeriod[];
  reason?: string | null;
}

/** Body of an OCPP command answer: 200 { message, status, response }. */
export interface OcppCommandAnswer<T> {
  message: string;
  status: string | null;
  response: T;
}

export interface EvChargingNeeds {
  id: number;
  evseId: number;
  requestedEnergyTransfer?: string | null;
  departureTime?: string | null;
  energyAmount?: number | null;
  evMinCurrent?: number | null;
  evMaxCurrent?: number | null;
  evMaxVoltage?: number | null;
  evMaxPower?: number | null;
  stateOfCharge?: number | null;
  receivedAt: string;
}

export interface StationLoadLimit {
  id?: number;
  chargingStationID: number;
  enabled: boolean;
  maxCurrentA?: number | null;
  maxPowerKW?: number | null;
  phases: number;
  voltage: number;
  minPerSessionA: number;
  strategy: LoadBalancingStrategy;
  safetyMarginPercent: number;
  lastRebalancedAt?: string | null;
}

export interface LoadBalancingStation {
  id: number;
  name: string;
  city?: string | null;
  chargePointCount: number;
  limit?: { enabled: boolean; maxCurrentA?: number | null; maxPowerKW?: number | null; strategy: LoadBalancingStrategy; lastRebalancedAt?: string | null } | null;
}

export interface SessionAllocationView {
  transactionID: number;
  transactionUid?: string | null;
  chargePointID: number;
  chargePointIdentity: string;
  evseId: number;
  connectorId?: number | null;
  startedAt: string;
  evMaxCurrentA?: number | null;
  allocatedA: number;
  allocatedKW: number;
  queued: boolean;
  sendStatus: string;
  reason?: string | null;
  lastSentAt?: string | null;
}

export interface RebalanceSummary {
  chargingStationID: number;
  enabled: boolean;
  stationLimitA: number;
  allocatedA: number;
  sent: number;
  unchanged: number;
  failed: number;
  sessions: SessionAllocationView[];
}

export interface StationLoadAllocation {
  id: number;
  transactionID: number;
  chargePointID: number;
  evseId: number;
  connectorId?: number | null;
  allocatedA: number;
  allocatedKW: number;
  stationLimitA: number;
  queued: boolean;
  sendStatus: string;
  reason?: string | null;
  createdAt: string;
}

export interface ChargingStrategyPeriod {
  startSeconds: number;
  limit: number;
  numberPhases?: number | null;
}

export interface ChargingStrategy {
  id: number;
  name: string;
  description?: string | null;
  purpose: ChargingProfilePurpose;
  kind: ChargingProfileKind;
  recurrencyKind?: ChargingProfileRecurrency | null;
  chargingRateUnit: ChargingRateUnit;
  stackLevel: number;
  periods: ChargingStrategyPeriod[];
  isPredefined: boolean;
  updatedAt: string;
}

export interface ChargingStrategyInput {
  name: string;
  description?: string | null;
  purpose: ChargingProfilePurpose;
  kind: ChargingProfileKind;
  recurrencyKind?: ChargingProfileRecurrency | null;
  chargingRateUnit: ChargingRateUnit;
  stackLevel: number;
  periods: ChargingStrategyPeriod[];
}

export interface StrategyApplyResult {
  chargePointID: number;
  chargePointIdentity: string;
  status: string;
  reason?: string | null;
  chargingProfileID?: number | null;
}

/** Smart charging: stored profiles, charger commands (ocpp/SmartCharging), station load balancing and strategies. */
@Injectable({
  providedIn: 'root'
})
export class SmartChargingService {

  private readonly apiUrl = environment.apiUrl + "/api/";
  private readonly ocppUrl = environment.apiUrl + "/ocpp/SmartCharging/";

  constructor(private _http: HttpClient) { }

  // Charging profiles
  getChargePoints(): Observable<SmartChargePoint[]> {
    return this._http.get<SmartChargePoint[]>(this.apiUrl + "ChargingProfile/GetChargePointsForSmartCharging");
  }

  getChargingProfiles(chargePointID: number, includeCleared: boolean): Observable<ChargingProfile[]> {
    const params = new HttpParams().set("chargePointID", chargePointID).set("includeCleared", includeCleared);
    return this._http.get<ChargingProfile[]>(this.apiUrl + "ChargingProfile/GetChargePointChargingProfiles", { params });
  }

  getEvChargingNeeds(chargePointID: number): Observable<EvChargingNeeds[]> {
    return this._http.get<EvChargingNeeds[]>(this.apiUrl + "ChargingProfile/GetChargePointEvChargingNeeds", { params: { chargePointID } });
  }

  sendChargingProfile(chargePointIdentity: string, input: ChargingProfileInput): Observable<OcppCommandAnswer<{ status: string; reason?: string; profile: ChargingProfile }>> {
    return this._http.post<OcppCommandAnswer<{ status: string; reason?: string; profile: ChargingProfile }>>(
      this.ocppUrl + "SendChargingProfile/" + encodeURIComponent(chargePointIdentity), input);
  }

  clearChargingProfile(chargePointIdentity: string, chargingProfileID: number): Observable<OcppCommandAnswer<{ status: string; clearedCount: number }>> {
    return this._http.post<OcppCommandAnswer<{ status: string; clearedCount: number }>>(
      this.ocppUrl + "ClearStoredChargingProfile/" + encodeURIComponent(chargePointIdentity), { chargingProfileID });
  }

  /** 2.0.1 only: the charger reports its profiles (ReportChargingProfiles), which are reconciled with the stored ones. */
  requestChargingProfilesReport(chargePointIdentity: string, evseId: number | null): Observable<OcppCommandAnswer<{ status: string; requestId: number }>> {
    return this._http.post<OcppCommandAnswer<{ status: string; requestId: number }>>(
      this.ocppUrl + "GetChargingProfiles/" + encodeURIComponent(chargePointIdentity), { requestId: 0, evseId, chargingProfile: {} });
  }

  getCompositeSchedule(chargePointIdentity: string, evseId: number, duration: number, chargingRateUnit: ChargingRateUnit | null): Observable<OcppCommandAnswer<CompositeSchedule>> {
    return this._http.post<OcppCommandAnswer<CompositeSchedule>>(
      this.ocppUrl + "GetCompositeSchedule/" + encodeURIComponent(chargePointIdentity), { evseId, duration, chargingRateUnit });
  }

  // Station load balancing
  getLoadBalancingStations(): Observable<LoadBalancingStation[]> {
    return this._http.get<LoadBalancingStation[]>(this.apiUrl + "StationLoadBalancing/GetLoadBalancingStations");
  }

  getStationLoadLimit(chargingStationID: number): Observable<StationLoadLimit> {
    return this._http.get<StationLoadLimit>(this.apiUrl + "StationLoadBalancing/GetStationLoadLimit", { params: { chargingStationID } });
  }

  saveStationLoadLimit(chargingStationID: number, limit: StationLoadLimit): Observable<StationLoadLimit> {
    return this._http.put<StationLoadLimit>(this.apiUrl + "StationLoadBalancing/SaveStationLoadLimit", limit, { params: { chargingStationID } });
  }

  getStationAllocations(chargingStationID: number): Observable<RebalanceSummary> {
    return this._http.get<RebalanceSummary>(this.apiUrl + "StationLoadBalancing/GetStationAllocations", { params: { chargingStationID } });
  }

  getStationAllocationHistory(chargingStationID: number, take = 50): Observable<StationLoadAllocation[]> {
    return this._http.get<StationLoadAllocation[]>(this.apiUrl + "StationLoadBalancing/GetStationAllocationHistory", { params: { chargingStationID, take } });
  }

  rebalanceStation(chargingStationID: number): Observable<RebalanceSummary> {
    return this._http.post<RebalanceSummary>(this.apiUrl + "StationLoadBalancing/RebalanceStation", {}, { params: { chargingStationID } });
  }

  // Charging strategies
  getChargingStrategies(): Observable<ChargingStrategy[]> {
    return this._http.get<ChargingStrategy[]>(this.apiUrl + "ChargingStrategy/GetChargingStrategies");
  }

  createChargingStrategy(input: ChargingStrategyInput): Observable<ChargingStrategy> {
    return this._http.post<ChargingStrategy>(this.apiUrl + "ChargingStrategy/CreateChargingStrategy", input);
  }

  updateChargingStrategy(id: number, input: ChargingStrategyInput): Observable<ChargingStrategy> {
    return this._http.put<ChargingStrategy>(this.apiUrl + "ChargingStrategy/UpdateChargingStrategy", input, { params: { id } });
  }

  deleteChargingStrategy(id: number): Observable<void> {
    return this._http.delete<void>(this.apiUrl + "ChargingStrategy/DeleteChargingStrategy", { params: { id } });
  }

  applyChargingStrategy(id: number, chargePointIDs: number[], chargingStationIDs: number[]): Observable<StrategyApplyResult[]> {
    return this._http.post<StrategyApplyResult[]>(this.apiUrl + "ChargingStrategy/ApplyChargingStrategy", { chargePointIDs, chargingStationIDs }, { params: { id } });
  }
}

/** Message of an API error: { error } (validation, 400/404) or { message } (OCPP transport errors). */
export function apiErrorMessage(error: any): string {
  return error?.error?.error || error?.error?.message || error?.message || "Something went wrong please contact your system administrator";
}

/** Seconds as h:mm (or d h:mm beyond a day). */
export function formatOffset(seconds: number): string {
  const days = Math.floor(seconds / 86400);
  const rest = seconds % 86400;
  const h = Math.floor(rest / 3600);
  const m = Math.floor((rest % 3600) / 60);
  const hm = h + ":" + String(m).padStart(2, "0");
  return days > 0 ? days + "d " + hm : hm;
}

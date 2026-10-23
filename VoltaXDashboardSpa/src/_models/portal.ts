/** Shapes of the customer portal (api/my), payments (api/payments) and partner portal (api/partner-portal) APIs. */

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface MyProfile {
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  birthday: string | null;
  imageUrl: string | null;
  isEmailVerified: boolean;
  isPhoneNumberVerified: boolean;
  memberSince: string;
}

export interface MyActiveSession {
  id: number;
  stationName: string | null;
  /** The charger's OCPP id (with transactionUid, what stopping the session needs). */
  chargePointName: string | null;
  connector: string | null;
  startDate: string;
  chargedMinutes: number;
  chargedKwhs: number;
  currentCost: number;
  transactionUid: string | null;
  /** Balance of the card the session is charged to, before this session. */
  cardBalance: number | null;
}

export interface MyOverview {
  firstName: string;
  walletBalance: number;
  cardCount: number;
  activeSession: MyActiveSession | null;
  sessionsThisMonth: number;
  kwhThisMonth: number;
  spentThisMonth: number;
  sessionsTotal: number;
  kwhTotal: number;
  spentTotal: number;
  currency: string;
}

export interface MyCard {
  id: number;
  maskedNumber: string;
  last4: string;
  balance: number;
  expirationDate: string;
  status: string;
  blocked: boolean;
  expired: boolean;
}

export interface MySession {
  id: number;
  stationName: string | null;
  stationAddress: string | null;
  chargePointName: string | null;
  connector: string | null;
  startDate: string;
  endDate: string | null;
  chargedMinutes: number;
  idleMinutes: number;
  chargedKwhs: number;
  energyAmount: number;
  timeAmount: number;
  flatFeeAmount: number;
  idleAmount: number;
  total: number;
  stoppedReason: string;
  status: string;
  active: boolean;
  invoiceAvailable: boolean;
}

export interface MySessionDetail {
  session: MySession;
  pricePerKWh: number | null;
  pricePerMinute: number | null;
  pricePerIdleMinute: number | null;
  flatFee: number | null;
  vatRate: number;
  totalExclVat: number;
  vat: number;
  balanceBefore: number | null;
  balanceAfter: number | null;
  cardLast4: string | null;
  currency: string;
}

export interface MySessionFilter {
  from?: string | null;
  to?: string | null;
  status?: '' | 'active' | 'completed';
  search?: string | null;
  page?: number;
  pageSize?: number;
}

export interface MyWalletMovement {
  id: number;
  createdAt: string;
  type: 'TopUp' | 'SessionCharge' | 'IdleFee' | 'Refund' | 'Adjustment';
  amount: number;
  balanceAfter: number;
  description: string | null;
  chargingSessionId: number | null;
  orderId: number | null;
  cardLast4: string;
}

export interface MyOrder {
  id: number;
  createdAt: string;
  amount: number;
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Canceled' | 'Refunded';
  provider: string | null;
  paidAt: string | null;
  failureReason: string | null;
  cardLast4: string;
  invoiceAvailable: boolean;
}

export interface PaymentConfig {
  provider: string;
  enabled: boolean;
  currency: string;
  minTopUp: number;
  maxTopUp: number;
  supportsSavedCards: boolean;
}

export interface CheckoutInstruction {
  method: 'GET' | 'POST';
  url: string;
  fields?: Record<string, string> | null;
  providerReference?: string | null;
}

export interface TopUpStatus {
  orderId: number;
  cardId: number;
  amount: number;
  status: MyOrder['status'];
  provider: string | null;
  createdAt: string;
  paidAt: string | null;
  failureReason: string | null;
}

export interface PaymentMethod {
  id: number;
  brand: 'Mastercard' | 'Visa' | 'Generic';
  last4: string;
  expiryMonth: number;
  expiryYear: number;
  isDefault: boolean;
  provider: string | null;
}

// ---------------------------------------------------------------------------- partner portal

export interface PartnerProfile {
  id: number;
  name: string;
  email: string | null;
  phone: string | null;
  city: string | null;
  address: string | null;
  logoUrl: string | null;
  stationCount: number;
  chargePointCount: number;
}

export interface PartnerOverview {
  stations: number;
  chargePoints: number;
  connectors: number;
  connectorsByStatus: Record<string, number>;
  activeSessions: number;
  revenueToday: number;
  revenueMonth: number;
  revenueTotal: number;
  energyMonth: number;
  sessionsMonth: number;
  currency: string;
}

export interface PartnerStation {
  id: number;
  name: string;
  address: string;
  city: string;
  status: string;
  chargePointCount: number;
  connectorCount: number;
  latitude: string | null;
  longitude: string | null;
}

export interface PartnerConnector {
  id: number;
  evseId: number;
  connectorId: number | null;
  type: string | null;
  powerKw: number;
  status: string | null;
  pricePerKWh: number;
  pricePerMinute: number;
  pricePerIdleMinute: number;
  flatFee: number;
}

export interface PartnerChargePoint {
  id: number;
  chargePointId: string;
  stationId: number;
  stationName: string | null;
  vendor: string | null;
  model: string | null;
  status: string;
  connectors: PartnerConnector[];
}

export interface ConnectorPricing {
  pricePerKWh: number;
  pricePerMinute: number;
  pricePerIdleMinute: number;
  flatFee: number;
}

export interface PartnerSession {
  id: number;
  stationName: string | null;
  chargePointName: string | null;
  connector: string | null;
  startDate: string;
  endDate: string | null;
  chargedMinutes: number;
  chargedKwhs: number;
  total: number;
  stoppedReason: string;
  active: boolean;
}

export interface PartnerSessionFilter {
  from?: string | null;
  to?: string | null;
  stationId?: number | null;
  chargePointId?: number | null;
  status?: '' | 'active' | 'completed';
  page?: number;
  pageSize?: number;
}

export interface PartnerStatisticPoint {
  period: string;
  sessions: number;
  energy: number;
  revenue: number;
}

export interface PartnerStatistics {
  daily: PartnerStatisticPoint[];
  monthly: PartnerStatisticPoint[];
  currency: string;
}

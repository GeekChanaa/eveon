/** Billing API shapes: refunds, invoices and credit notes, partner settlements (see docs/billing.md). */

export type RefundTarget = 'Order' | 'Session';
export type RefundMethod = 'Gateway' | 'Wallet' | 'External';
export type RefundStatus = 'Pending' | 'Succeeded' | 'Failed';
export type SettlementStatus = 'Pending' | 'Paid';

export interface Refund {
  id: number;
  target: RefundTarget;
  orderId: number | null;
  chargingSessionId: number | null;
  cardId: number;
  amount: number;
  reason: string;
  method: RefundMethod;
  status: RefundStatus;
  provider: string | null;
  providerReference: string | null;
  failureReason: string | null;
  requestedByUserId: number;
  requestedBy: string | null;
  createdAt: string;
  completedAt: string | null;
  creditNoteId: number | null;
  creditNoteNumber: string | null;
}

/** What is left to refund on a top-up or a session, and how it can be refunded. */
export interface RefundSummary {
  target: RefundTarget;
  id: number;
  paid: number;
  refunded: number;
  refundable: number;
  cardBalance: number;
  methods: RefundMethod[];
  defaultMethod: RefundMethod | null;
  provider: string | null;
  note: string | null;
  refunds: Refund[];
}

export interface RefundRequest {
  amount: number;
  reason: string;
  idempotencyKey: string;
  method?: RefundMethod | null;
}

export interface InvoiceRow {
  id: number;
  kind: 'Invoice' | 'CreditNote';
  number: string;
  issuedAt: string;
  customerName: string;
  amountExclVat: number;
  vatAmount: number;
  amountInclVat: number;
  vatRate: number;
  currency: string;
  language: string;
  chargingSessionId: number | null;
  orderId: number | null;
  refundId: number | null;
  creditedInvoiceNumber: string | null;
  description: string | null;
}

export interface InvoiceFilter {
  from?: string | null;
  to?: string | null;
  kind?: '' | 'Invoice' | 'CreditNote';
  search?: string | null;
  page: number;
  pageSize: number;
}

export interface PartnerCommission {
  partnerId: number;
  name: string;
  commissionPercent: number;
  commissionPerKwh: number;
}

export interface Settlement {
  id: number;
  partnerId: number;
  partnerName: string;
  year: number;
  month: number;
  periodStart: string;
  periodEnd: string;
  sessionCount: number;
  energyKwh: number;
  grossAmount: number;
  netExclVat: number;
  revenueVat: number;
  commissionExclVat: number;
  commissionVat: number;
  netPayout: number;
  commissionPercent: number;
  commissionPerKwh: number;
  vatRate: number;
  status: SettlementStatus;
  paidAt: string | null;
  paymentReference: string | null;
  generatedAt: string;
}

export interface SettlementLine {
  id: number;
  sourceKey: string;
  chargingSessionId: number | null;
  refundId: number | null;
  date: string;
  station: string | null;
  chargePoint: string | null;
  kwh: number;
  gross: number;
  netExclVat: number;
  vat: number;
  commissionExclVat: number;
  commissionVat: number;
  netPayout: number;
}

export interface SettlementDetail {
  settlement: Settlement;
  lines: SettlementLine[];
}

export interface SettlementFilter {
  partnerId?: number | null;
  year?: number | null;
  month?: number | null;
  status?: '' | SettlementStatus;
  page: number;
  pageSize: number;
}

/** A random key for one refund the operator confirms (the API refunds once per key). */
export function newIdempotencyKey(): string {
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  return Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('');
}

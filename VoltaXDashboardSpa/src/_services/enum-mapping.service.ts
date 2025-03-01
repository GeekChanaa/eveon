import { Injectable } from '@angular/core';
import { PartnerTypeEnum } from 'src/_models/_enums/partner-enum-type';

@Injectable({
  providedIn: 'root',
})
export class EnumMappingService {
  constructor() {}

  private enumMappings: { [key: string]: { [id: number]: string } } = {
    CardStatus: {
      0: 'Active',
      1: 'Inactive',
      2: 'Blocked',
      3: 'Expired',
    },
    CardType: {
      0: 'Standard',
      1: 'Premium',
      2: 'Partner',
    },
    ChargePointCategory: {
      0: 'TheTower',
      1: 'TheTowerPlus',
      2: 'VXCommercial',
      3: 'TheTowerDC',
      4: 'VXHome',
    },
    ChargePointStatus: {
      0: 'Available',
      1: 'Offline',
      2: 'UnderMaintenance',
    },
    UserRole: {
      0: 'Admin',
      1: 'Customer',
      2: 'PremiumCustomer',
      3: 'Support',
    },
    ChargingStationCategoryEnum: {
      0: 'Public',
      1: 'Private',
      2: 'Partner'
    },
    ChargingStationStatusEnum: {
      0: 'Available',
      1: 'UnderMaintenance',
      2: 'Offline'
    },
    ChargingStationNetworkEnum: {
      0: 'Public',
      1: 'Private',
      2: 'Partner'
    },
    ParkingTypeEnum: {
      0: 'ParallelParking',
      1: 'PerpendicularParking',
      2: 'AngleParking'
    },
    ReportStatus: {
      0: 'Pending',
      1: 'InProgress',
      2: 'Resolved',
      3: 'Closed',
    },
    ReportType: {
      0: 'ChargePoint',
      1: 'Connector',
      2: 'Website',
      3: 'Other',
    },
    ReportCategory: {
      0: 'General',
      1: 'Technical',
      2: 'Maintenance',
    },
    PartnerTypeEnum: {
      0: 'Vendor',
      1: 'ServiceProvider',
      2: 'Reseller',
      3: 'Affiliate',
      4: 'Investor',
      5: 'Other',
    },
    ReportCriticality: {
      0: 'Informational',
      1: 'Low',
      2: 'Medium',
      3: 'High',
      4: 'Critical'
    },
    RechargeOrderStatus: {
      0: 'Pending',
      1: 'Processing',
      2: 'Completed',
      3: 'Failed',
      4: 'Canceled',
      5: 'Refunded'
    },
    ReportStatusEnum: {
      0: 'Pending',
      1: 'InProgress',
      2: 'Resolved',
      3: 'Closed'
    }
  };

  getEnumMapping(modelName: string): { [id: number]: string } {
    return this.enumMappings[modelName];
  }
}

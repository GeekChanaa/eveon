import { Injectable } from '@angular/core';

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
      4: 'Partner',
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
  };

  getEnumMapping(modelName: string): { [id: number]: string } {
    return this.enumMappings[modelName];
  }
}

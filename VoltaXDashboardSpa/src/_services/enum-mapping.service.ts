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
      0: 'The Tower',
      1: 'The Tower Plus',
      2: 'VX Commercial',
      3: 'The Tower DC',
      4: 'VX Home',
    },
    ChargePointStatus: {
      0: 'Available',
      1: 'Offline',
      2: 'Under Maintenance',
    },
    UserRole: {
      0: 'Admin',
      1: 'Customer',
      2: 'Premium Customer',
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
    ParkingTypeEnum: {
      0: 'ParallelParking',
      1: 'PerpendicularParking',
      2: 'AngleParking'
    },
  };

  getEnumMapping(modelName: string): { [id: number]: string } {
    return this.enumMappings[modelName];
  }
}

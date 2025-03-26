import { PartnerTypeEnum } from "./_enums/partner-enum-type";

export interface Partner {
    id: number;
    name: string;
    description: string;
    type: PartnerTypeEnum;
    email: string;
    email2?: string;
    email3?: string;
    phone: string;
    phone2?: string;
    phone3?: string;
    city?: string;
    country?: string;
    address?: string;
    taxIdentificationNumber?: string;
    registrationNumber?: string;
    bankAccountNumber?: string;
    logoUrl?: string;
    isDeleted: boolean;
    createdAt: Date;
    updatedAt: Date;
  }
  
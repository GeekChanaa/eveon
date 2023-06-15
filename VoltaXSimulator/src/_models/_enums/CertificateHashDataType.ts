import { CustomDataType } from "../CustomDataType";
import { HashAlgorithmEnumType } from "./HashAlgorithmEnumType";

export interface CertificateHashDataType {
    customData?: CustomDataType;
    hashAlgorithm: HashAlgorithmEnumType;
    issuerNameHash: string;
    issuerKeyHash: string;
    serialNumber: string;
  }
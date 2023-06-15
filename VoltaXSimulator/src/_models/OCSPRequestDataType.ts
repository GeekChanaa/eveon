import { CustomDataType } from "./CustomDataType";
import { HashAlgorithmEnumType } from "./_enums/HashAlgorithmEnumType";

export interface OCSPRequestDataType {
    customData?: CustomDataType;
    hashAlgorithm: HashAlgorithmEnumType;
    issuerNameHash: string;
    issuerKeyHash: string;
    serialNumber: string;
    responderURL: string;
  }
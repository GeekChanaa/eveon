import { CustomDataType } from "./CustomDataType";
import { SignedMeterValueType } from "./SignedMeterValueType";
import { UnitOfMeasureType } from "./UnitOfMeasureType";
import { LocationEnumType } from "./_enums/LocationEnumType";
import { MeasurandEnumType } from "./_enums/MeasurandEnumType";
import { PhaseEnumType } from "./_enums/PhaseEnumType";
import { ReadingContextEnumType } from "./_enums/ReadingContextEnumType";

  
  export interface SampledValueType {
    customData?: CustomDataType;
    value: number;
    context: ReadingContextEnumType;
    measurand: MeasurandEnumType;
    phase: PhaseEnumType;
    location: LocationEnumType;
    signedMeterValue?: SignedMeterValueType;
    unitOfMeasure?: UnitOfMeasureType;
  }
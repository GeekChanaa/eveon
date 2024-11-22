using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace VoltaXApi.OCPP.Messages
{
    public class MeterValuesRequest
    {
        public CustomDataType CustomData { get; set; }

        [Required]
        public int EvseId { get; set; }

        [Required]
        [MinLength(1)]
        public List<MeterValueType> MeterValue { get; set; }
    }

    public enum LocationEnumType
    {
        Body,
        Cable,
        EV,
        Inlet,
        Outlet,
    }

    public enum MeasurandEnumType
    {
        Current_Export,
        Current_Import,
        Current_Offered,
        Energy_Active_Export_Register,
        Energy_Active_Import_Register,
        Energy_Reactive_Export_Register,
        Energy_Reactive_Import_Register,
        Energy_Active_Export_Interval,
        Energy_Active_Import_Interval,
        Energy_Active_Net,
        Energy_Reactive_Export_Interval,
        Energy_Reactive_Import_Interval,
        Energy_Reactive_Net,
        Energy_Apparent_Net,
        Energy_Apparent_Import,
        Energy_Apparent_Export,
        Frequency,
        Power_Active_Export,
        Power_Active_Import,
        Power_Factor,
        Power_Offered,
        Power_Reactive_Export,
        Power_Reactive_Import,
        SoC,
        Voltage,
        Missing,
    }

    public enum PhaseEnumType
    {
        L1,
        L2,
        L3,
        N,
        L1_N,
        L2_N,
        L3_N,
        L1_L2,
        L2_L3,
        L3_L1,
    }

    public enum ReadingContextEnumType
    {
        Interruption_Begin,
        Interruption_End,
        Other,
        Sample_Clock,
        Sample_Periodic,
        Transaction_Begin,
        Transaction_End,
        Trigger,
    }

    public class MeterValueType
    {
        [Required]
        public List<SampledValueType> SampledValue { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public CustomDataType CustomData { get; set; }
    }

    public class SampledValueType
    {
        [Required]
        public double Value { get; set; }

        public CustomDataType CustomData { get; set; }

        [JsonConverter(typeof(ReadingContextEnumConverter))]
        public ReadingContextEnumType? Context { get; set; }
        [JsonConverter(typeof(MeasurandEnumConverter))]
        public MeasurandEnumType? Measurand { get; set; }
        public PhaseEnumType? Phase { get; set; }
        public LocationEnumType? Location { get; set; }
        public SignedMeterValueType SignedMeterValue { get; set; }
        public UnitOfMeasureType UnitOfMeasure { get; set; }
    }

    public class SignedMeterValueType
    {
        [Required]
        public string SignedMeterData { get; set; }

        [Required]
        public string SigningMethod { get; set; }

        [Required]
        public string EncodingMethod { get; set; }

        [Required]
        public string PublicKey { get; set; }

        public CustomDataType CustomData { get; set; }
    }

    public class UnitOfMeasureType
    {
        public CustomDataType CustomData { get; set; }

        [MaxLength(20)]
        public string Unit { get; set; } = "Wh";

        public int Multiplier { get; set; } = 0;
    }
}

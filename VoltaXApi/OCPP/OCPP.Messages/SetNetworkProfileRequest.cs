using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SetNetworkProfileRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public int ConfigurationSlot { get; set; }

      [Required]
      public NetworkConnectionProfileType ConnectionData { get; set; }
  }

  public enum APNAuthenticationEnumType
  {
      CHAP,
      NONE,
      PAP,
      AUTO
  }

  public enum OCPPInterfaceEnumType
  {
      Wired0,
      Wired1,
      Wired2,
      Wired3,
      Wireless0,
      Wireless1,
      Wireless2,
      Wireless3
  }

  public enum OCPPTransportEnumType
  {
      JSON,
      SOAP
  }

  public enum OCPPVersionEnumType
  {
      OCPP12,
      OCPP15,
      OCPP16,
      OCPP20
  }

  public enum VPNEnumType
  {
      IKEv2,
      IPSec,
      L2TP,
      PPTP
  }

  public class APNType
  {
      [Required]
      [StringLength(512)]
      public string Apn { get; set; }

      [Required]
      public APNAuthenticationEnumType ApnAuthentication { get; set; }

      [StringLength(20)]
      public string ApnUserName { get; set; }

      [StringLength(20)]
      public string ApnPassword { get; set; }

      public int? SimPin { get; set; }

      [StringLength(6)]
      public string PreferredNetwork { get; set; }

      public bool UseOnlyPreferredNetwork { get; set; } = false;

      public CustomDataType CustomData { get; set; }
  }

  public class NetworkConnectionProfileType
  {
      [Required]
      public OCPPVersionEnumType OcppVersion { get; set; }

      [Required]
      public OCPPTransportEnumType OcppTransport { get; set; }

      [Required]
      [StringLength(512)]
      public string OcppCsmsUrl { get; set; }

      [Required]
      public int MessageTimeout { get; set; }

      [Required]
      public int SecurityProfile { get; set; }

      [Required]
      public OCPPInterfaceEnumType OcppInterface { get; set; }

      public CustomDataType CustomData { get; set; }

      public APNType Apn { get; set; }

      public VPNType Vpn { get; set; }
  }

  public class VPNType
  {
      [Required]
      [StringLength(512)]
      public string Server { get; set; }

      [Required]
      [StringLength(20)]
      public string User { get; set; }

      [StringLength(20)]
      public string Group { get; set; }

      [Required]
      [StringLength(20)]
      public string Password { get; set; }

      [Required]
      [StringLength(255)]
      public string Key { get; set; }

      [Required]
      public VPNEnumType Type { get; set; }

      public CustomDataType CustomData { get; set; }
  }

}
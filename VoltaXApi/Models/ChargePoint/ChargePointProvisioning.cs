namespace VoltaXApi.Models
{
    /// <summary>
    /// Records that a charge point went through first-connection provisioning. A charge point
    /// without a row is answered Pending on BootNotification until it is provisioned.
    /// Kept out of <see cref="ChargePoint"/> so the generic charge point update cannot reset it.
    /// </summary>
    public class ChargePointProvisioning : IEntity
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }
        public ChargePoint? ChargePoint { get; set; }
        public ChargePointProvisioningStatusEnum Status { get; set; }
        public ChargePointProvisioningMethodEnum Method { get; set; }
        public DateTime ProvisionedAt { get; set; }
        public int? ProvisionedByUserID { get; set; }
        public User? ProvisionedByUser { get; set; }
        public int AcceptedCount { get; set; }
        public int FailedCount { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

namespace LedgerProject.Models
{
    public class AuditEvent
    {
        public Guid EventId { get; set; }
        public string EventType { get; set; }
        public string EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string PerformedBy { get; set; }
        public DateTime EventTime { get; set; }
        public string Description { get; set; }
    }
}
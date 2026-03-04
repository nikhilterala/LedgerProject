using LedgerProject.Data;
using LedgerProject.Models;

namespace LedgerProject.Services
{
    public class AuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogEventAsync(string eventType,string entityType,Guid entityId,string performedBy,string description)
        {
            var audit = new AuditEvent
            {
                EventId = Guid.NewGuid(),
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                PerformedBy = performedBy,
                EventTime = DateTime.UtcNow,
                Description = description
            };

            _context.AuditEvents.Add(audit);
            await _context.SaveChangesAsync();
        }
    }
}
using DS.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers
{
    [Authorize(Roles = nameof(AppRoles.EmailOutboxView))]
    [Route("api/v1/email-outbox")]
    public class EmailOutboxApiController(DataDbContext dataDb) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 50;

            var all = dataDb.EmailOutbox.AsNoTracking();

            var filtered = status switch
            {
                "pending" => all.Where(m => m.SentAt == null && m.FailedAt == null),
                "locked" => all.Where(m => m.SentAt == null && m.FailedAt == null && m.LockedAt != null),
                "failed" => all.Where(m => m.FailedAt != null),
                "sent" => all.Where(m => m.SentAt != null),
                _ => all
            };

            var items = await filtered
                .OrderByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new EmailOutboxDto
                {
                    Id = m.Id,
                    EventType = m.EventType,
                    CorrelationId = m.CorrelationId,
                    ToEmail = m.ToEmail,
                    Subject = m.Subject,
                    Body = m.Body,
                    CreatedAt = m.CreatedAt,
                    NextAttemptAt = m.NextAttemptAt,
                    SentAt = m.SentAt,
                    FailedAt = m.FailedAt,
                    LockedAt = m.LockedAt,
                    LockedBy = m.LockedBy,
                    Attempts = m.Attempts,
                    LastError = m.LastError
                })
                .ToListAsync();

            var total = await filtered.CountAsync();

            var pendingCount = await all.CountAsync(m => m.SentAt == null && m.FailedAt == null);
            var lockedCount = await all.CountAsync(m => m.SentAt == null && m.FailedAt == null && m.LockedAt != null);
            var failedCount = await all.CountAsync(m => m.FailedAt != null);

            return Ok(new EmailOutboxPageDto
            {
                Items = items,
                Total = total,
                PendingCount = pendingCount,
                LockedCount = lockedCount,
                FailedCount = failedCount
            });
        }
    }
}

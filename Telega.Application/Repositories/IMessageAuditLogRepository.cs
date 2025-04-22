
using Telega.Domain.Entities;

namespace Telega.Application.Repositories
{
    public interface IMessageAuditLogRepository
    {
        Task AddAsync(MessageAuditLog auditLog);
    }
}

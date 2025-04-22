using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telega.Application.Repositories;
using Telega.Domain.Entities;
using Telega.Infrastructure.Data;

namespace Telega.Infrastructure.Repositories
{
    public class MessageAuditLogRepository : IMessageAuditLogRepository
    {
        private readonly AppDbContext _dbContext;

        public MessageAuditLogRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(MessageAuditLog auditLog)
        {
            await _dbContext.MessageAuditLogs.AddAsync(auditLog);
            await _dbContext.SaveChangesAsync();
        }
    }
}

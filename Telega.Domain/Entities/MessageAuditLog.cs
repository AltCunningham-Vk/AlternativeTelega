using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telega.Domain.Entities
{
    public enum AuditAction
    {
        Created,
        MarkedAsRead,
        Removed
    }
    public class MessageAuditLog :BaseEntity
    {
        public Guid MessageId { get; private set; }
        public Guid UserID { get; private set; }
        public AuditAction Action { get; private set; }
        public DateTime TimeStamp { get; private set; }
        private MessageAuditLog()
        {
        }

        public MessageAuditLog(Guid messageId, Guid userId, AuditAction action)
        {
            MessageId = messageId;
            UserID = userId;
            Action = action;
            TimeStamp = DateTime.UtcNow;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telega.Domain.Entities
{
    //Базовый класс для всех сущностей
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;


        public void UpdateTimeStamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

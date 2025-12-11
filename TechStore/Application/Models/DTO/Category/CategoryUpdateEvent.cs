using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.DTO.Category
{
    public class CategoryUpdateEvent
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public DateTime lastChanged { get; set; } = DateTime.UtcNow;
    }
}

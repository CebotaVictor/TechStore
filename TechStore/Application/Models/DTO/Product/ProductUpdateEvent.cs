using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.DTO.Product
{
    public class ProductUpdateEvent
    {
        public string? Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public decimal? Price { get; set; } = 0.0m;
        //public string? CategoryId { get; set; } = string.Empty;
        public DateTime lastChanged { get; set; } = DateTime.UtcNow;
    }

}

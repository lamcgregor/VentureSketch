using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VentureSketch.Models
{
    public class Qualification
    {
        public int Id { get; set; }
        public int? Rank { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        public int ActivityTypeId { get; set; }
        public ActivityType ActivityType { get; set; }
    }
}

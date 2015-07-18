using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VentureSketch.Models
{
    public class UserQualification
    {
        public int Id { get; set; }
        public int QualificationId { get; set; }
        public int UserId { get; set; }
    }
}

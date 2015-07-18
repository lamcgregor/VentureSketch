using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VentureSketch.Models
{
    public class NationalGoverningBody
    {
        public int Id { get; set; }
        public string Abbreviation { get; set; }
        public string Name { get; set; }
        public List<Qualification> QualificationsNationalGoverningBodies { get; set; }
    }
}

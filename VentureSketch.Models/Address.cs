using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VentureSketch.Models
{
    public class Address
    {
        public int CountryId { get; set; }
        public int Id { get; set; }
        public int UserId { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string PostCode { get; set; }
    }
}

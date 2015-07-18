using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VentureSketch.Models
{
    public class JobProfile
    {
        public bool IsTemporary { get; set; }
        public bool RequiresOwnInsurance { get; set; }
        public byte[] ProfilePicture { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public decimal HourlyPayRate { get; set; }
        public decimal TravelDistance { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Notes { get; set; }
        public string PostCode { get; set; }
    }
}

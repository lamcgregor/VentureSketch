using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VentureSketch.Models
{
    public class Match
    {
        public DateTime MatchDateTime { get; set; }
        public int Id { get; set; }
        public int JobProfileId { get; set; }
        public int MatchingJobProfileId { get; set; }
    }
}

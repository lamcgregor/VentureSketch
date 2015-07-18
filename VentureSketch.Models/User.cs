using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VentureSketch.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public int? UserTypeId { get; set; }
        public UserType UserType { get; set; }
        public List<Qualification> UserQualifications { get; set; }
    }
}
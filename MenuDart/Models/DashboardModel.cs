using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace MenuDart.Models
{
    public class DashboardModel
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        public int ID { get; set; }
        public string Email { get; set; }
        public double SignUpDate { get; set; }     //Unix Timestamp
        public bool TrialEnded { get; set; }
        public bool Subscribed { get; set; }
        public bool CouponActive { get; set; }
        public string Feedback { get; set; }
        public IEnumerable<MenuDart.Models.Menu> Menus { get; set; } 
    }
}
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
        public string Email { get; set; }
        public bool TrialEnded { get; set; }
        public bool Subscribed { get; set; }
        public string Feedback { get; set; }
        public IEnumerable<MenuDart.Models.Menu> Menus { get; set; } 
    }
}
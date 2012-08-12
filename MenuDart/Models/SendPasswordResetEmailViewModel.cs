using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MenuDart.Models
{
    public class SendPasswordResetEmailViewModel
    {
        public string Email { get; set; }
        public string ResetLink { get; set; }
    }
}
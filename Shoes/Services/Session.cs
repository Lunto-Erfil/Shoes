using Shoes.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shoes.Services
{
    public static class Session
    {
        public static User CurrentUser { get; set; }

        public static bool IsGuest => CurrentUser == null;
        public static bool IsAdmin => CurrentUser?.IsAdmin == true;
        public static bool IsManager => CurrentUser?.IsManager == true;
    }
}
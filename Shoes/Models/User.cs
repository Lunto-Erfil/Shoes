using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shoes.Models
{
    public class User
    {
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Login { get; set; }
        public string RoleName { get; set; }

        public string FullName =>
            $"{LastName} {FirstName} {MiddleName}".Trim();

        public bool IsAdmin => RoleName == "Администратор";
        public bool IsManager => RoleName == "Менеджер" || IsAdmin;
        public bool IsClient => RoleName == "Авторизированный клиент";
    }
}
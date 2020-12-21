﻿using System;
using System.Collections.Generic;

#nullable disable

namespace KokazGoodsTransfer.Models
{
    public partial class User
    {
        public User()
        {
            Clients = new HashSet<Client>();
            UserGroups = new HashSet<UserGroup>();
            UserPhones = new HashSet<UserPhone>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int DepartmentId { get; set; }
        public string Experince { get; set; }
        public string Adress { get; set; }
        public string HireDate { get; set; }
        public string Note { get; set; }
        public bool CanWorkAsAgent { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public virtual Department Department { get; set; }
        public virtual ICollection<Client> Clients { get; set; }
        public virtual ICollection<UserGroup> UserGroups { get; set; }
        public virtual ICollection<UserPhone> UserPhones { get; set; }
    }
}

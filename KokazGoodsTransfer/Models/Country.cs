﻿using System;
using System.Collections.Generic;

#nullable disable

namespace KokazGoodsTransfer.Models
{
    public partial class Country
    {
        public Country()
        {
            Orders = new HashSet<Order>();
            Regions = new HashSet<Region>();
            Users = new HashSet<User>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public decimal DeliveryCost { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Region> Regions { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}

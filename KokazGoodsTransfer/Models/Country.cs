﻿using System;
using System.Collections.Generic;

#nullable disable

namespace KokazGoodsTransfer.Models
{
    public partial class Country
    {
        public Country()
        {
            AgentCountrs = new HashSet<AgentCountr>();
            Clients = new HashSet<Client>();
            OrderLogs = new HashSet<OrderLog>();
            Orders = new HashSet<Order>();
            Regions = new HashSet<Region>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public decimal DeliveryCost { get; set; }

        public virtual ICollection<AgentCountr> AgentCountrs { get; set; }
        public virtual ICollection<Client> Clients { get; set; }
        public virtual ICollection<OrderLog> OrderLogs { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Region> Regions { get; set; }
    }
}

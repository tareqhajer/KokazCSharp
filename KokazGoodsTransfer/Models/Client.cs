﻿using System;
using System.Collections.Generic;

#nullable disable

namespace KokazGoodsTransfer.Models
{
    public partial class Client
    {
        public Client()
        {
            ClientPhones = new HashSet<ClientPhone>();
            Markets = new HashSet<Market>();
            OrderLogs = new HashSet<OrderLog>();
            Orders = new HashSet<Order>();
            Receipts = new HashSet<Receipt>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int? CountryId { get; set; }
        public string Address { get; set; }
        public DateTime FirstDate { get; set; }
        public string Note { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int UserId { get; set; }
        public string Mail { get; set; }

        public virtual Country Country { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<ClientPhone> ClientPhones { get; set; }
        public virtual ICollection<Market> Markets { get; set; }
        public virtual ICollection<OrderLog> OrderLogs { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Receipt> Receipts { get; set; }
    }
}

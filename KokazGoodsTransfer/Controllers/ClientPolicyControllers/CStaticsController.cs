﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using KokazGoodsTransfer.ClientDtos;
using KokazGoodsTransfer.Models;
using KokazGoodsTransfer.Models.Static;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KokazGoodsTransfer.Controllers.ClientPolicyControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CStaticsController : AbstractClientPolicyController
    {
        public CStaticsController(KokazContext context, IMapper mapper) : base(context, mapper)
        {
        }
        [HttpGet]
        public IActionResult Get()
        {
            var orders = this.Context.Orders.Where(c => c.ClientId == AuthoticateUserId());
            var ordersInsiedCompany = orders.Where(c => c.MoenyPlacedId == (int)MoneyPalcedEnum.InsideCompany);
            StaticsDto staticsDto = new StaticsDto
            {
                TotalOrder = orders.Count(),
                OrderMoneyInCompany = ordersInsiedCompany.Where(c => c.OrderplacedId == (int)OrderplacedEnum.Delivered || c.OrderplacedId == (int)OrderplacedEnum.PartialReturned).Count(),
                OrderDeliverdToClient = orders.Where(c => c.OrderStateId != (int)OrderStateEnum.Finished && c.IsClientDiliverdMoney == true).Count(),
                OrderInWat = orders.Where(c=>c.OrderplacedId==(int)OrderplacedEnum.Way).Count(),
                OrderInStore = orders.Where(c => c.OrderplacedId == (int)OrderplacedEnum.Store).Count(),


            };
            //var orderInCompany = orders.Where(c => c.MoenyPlacedId == (int)MoneyPalcedEnum.InsideCompany && c.OrderplacedId != (int)OrderplacedEnum.Delayed&&c.OrderplacedId!=(int)OrderplacedEnum.CompletelyReturned);
            //StaticsDto staticsDto = new StaticsDto
            //{
            //    TotalOrder = orders.Count(),
            //    OrderWithClient = orders.Where(c => c.OrderplacedId == (int)OrderplacedEnum.Client).Count(),
            //    OrderInWay = orders.Where(c => c.OrderplacedId == (int)OrderplacedEnum.Way).Count(),
            //    OrderInCompany = orderInCompany.Count(),
            //    DeliveredOrder = orderInCompany.Where(c => c.OrderplacedId == (int)OrderplacedEnum.Delivered).Count(),
            //    UnacceptableOrder = orderInCompany.Where(c => c.OrderplacedId == (int)OrderplacedEnum.Unacceptable).Count(),
            //    CompletelyReturnedOrder = orderInCompany.Where(c => c.OrderplacedId == (int)OrderplacedEnum.CompletelyReturned).Count(),
            //    PartialReturnedOrder = orderInCompany.Where(c => c.OrderplacedId == (int)OrderplacedEnum.PartialReturned).Count(),
            //    DelayedOrder = orders.Where(c => c.OrderplacedId == (int)OrderplacedEnum.Delayed).Count(),
            //};
            return Ok(staticsDto);
        }
    }
}
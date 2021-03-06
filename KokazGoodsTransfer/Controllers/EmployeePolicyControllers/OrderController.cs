﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using KokazGoodsTransfer.Dtos.Common;
using KokazGoodsTransfer.Dtos.OrdersDtos;
using KokazGoodsTransfer.Models;
using KokazGoodsTransfer.Models.Static;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KokazGoodsTransfer.Controllers.EmployeePolicyControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : AbstractEmployeePolicyController
    {
        public OrderController(KokazContext context, IMapper mapper) : base(context, mapper)
        {
        }
        [HttpPost]
        public IActionResult Create([FromBody] CreateOrdersFromEmployee createOrdersFromEmployee)
        {
            var country = this.Context.Countries.Find(createOrdersFromEmployee.CountryId);
            var dbContextTransaction = this.Context.Database.BeginTransaction();
            try
            {
                var order = mapper.Map<CreateOrdersFromEmployee, Order>(createOrdersFromEmployee);

                order.CreatedBy = AuthoticateUserName();
                if (this.Context.Orders.Where(c => c.Code == order.Code && c.ClientId == order.ClientId).Any())
                {
                    return Conflict();
                }

                if (createOrdersFromEmployee.RegionId == null)
                {
                    var region = new Region()
                    {
                        Name = createOrdersFromEmployee.RegionName,
                        CountryId = createOrdersFromEmployee.CountryId
                    };
                    this.Context.Add(region);
                    this.Context.SaveChanges();
                    order.RegionId = region.Id;
                    order.Seen = true;

                    order.AgentCost = this.Context.Users.Find(order.AgentId).Salary ?? 0;
                }
                order.OrderStateId = (int)OrderStateEnum.Processing;
                order.DeliveryCost = country.DeliveryCost;
                if (order.MoenyPlacedId == (int)MoneyPalcedEnum.Delivered)
                {
                    order.IsClientDiliverdMoney = true;

                }
                else
                {
                    order.IsClientDiliverdMoney = false;
                }

                this.Context.Add(order);
                this.Context.SaveChanges();

                if (createOrdersFromEmployee.OrderTypeDtos != null)
                {

                    foreach (var item in createOrdersFromEmployee.OrderTypeDtos)
                    {
                        int orderId;
                        if (item.OrderTypeId != null)
                        {
                            orderId = (int)item.OrderTypeId;
                        }
                        else
                        {
                            OrderType orderType = new OrderType()
                            {
                                Name = item.OrderTypeName
                            };
                            this.Context.Add(orderType);
                            this.Context.SaveChanges();
                            orderId = orderType.Id;
                        }
                        OrderItem orderItem = new OrderItem()
                        {
                            OrderId = order.Id,
                            Count = item.Count,
                            OrderTpyeId = orderId
                        };
                        this.Context.Add(orderItem);
                        this.Context.SaveChanges();
                    }
                }

                dbContextTransaction.Commit();
                return Ok();
            }
            catch (Exception ex)
            {
                dbContextTransaction.Rollback();
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch]
        public IActionResult Edit([FromBody] UpdateOrder updateOrder)
        {

            try
            {
                var order = this.Context.Orders.Find(updateOrder.Id);
                OrderLog log = order;
                this.Context.Add(log);
                if (order.Code != updateOrder.Code)
                {
                    if (this.Context.Orders.Any(c => c.ClientId == order.ClientId && c.Code == updateOrder.Code))
                    {
                        return Conflict(new { Message = "الكود مستخدم سابقاً" });
                    }
                }
                order.Code = updateOrder.Code;

                if (order.AgentId != updateOrder.AgentId)
                {
                    order.OrderStateId = (int)OrderStateEnum.Processing;
                    order.MoenyPlacedId = (int)MoneyPalcedEnum.OutSideCompany;
                    order.OrderplacedId = (int)OrderplacedEnum.Store;
                }
                if (order.ClientId != updateOrder.ClientId)
                {
                    if (order.IsClientDiliverdMoney)
                    {
                        order.IsClientDiliverdMoney = false;
                        Receipt receipt = new Receipt()
                        {
                            IsPay = true,
                            ClientId = order.ClientId,
                            Amount = ((order.Cost - order.DeliveryCost) * -1),
                            CreatedBy = "النظام",
                            Manager = "",
                            Date = DateTime.Now,
                            About = "",
                            Note = " بعد تعديل طلب بكود " + order.Code,
                        };
                        this.Context.Add(receipt);
                    }
                }
                order.DeliveryCost = updateOrder.DeliveryCost;
                order.Cost = updateOrder.Cost;
                order.ClientId = updateOrder.ClientId;
                order.AgentId = updateOrder.AgentId;
                order.CountryId = updateOrder.CountryId;
                order.RegionId = updateOrder.RegionId;
                order.Address = updateOrder.Address;
                order.RecipientName = updateOrder.RecipientName;
                order.RecipientPhones = String.Join(",", updateOrder.RecipientPhones);
                order.Note = updateOrder.Note;
                this.Context.Update(order);
                this.Context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
                //return BadRequest(new { ex.Message, errorLine });
            }
        }
        [HttpPost("createMultiple")]
        public IActionResult Create([FromBody] List<CreateMultipleOrder> createMultipleOrders)
        {
            try
            {
                foreach (var item in createMultipleOrders)
                {
                    var order = mapper.Map<Order>(item);
                    var country = this.Context.Countries.Find(order.CountryId);
                    order.Seen = true;
                    order.MoenyPlacedId = (int)MoneyPalcedEnum.OutSideCompany;
                    order.IsClientDiliverdMoney = false;
                    order.OrderStateId = (int)OrderStateEnum.Processing;
                    order.AgentCost = this.Context.Users.Find(order.AgentId).Salary ?? 0;
                    order.Date = item.Date;
                    order.OrderplacedId = (int)OrderplacedEnum.Store;
                    order.CreatedBy = AuthoticateUserName();
                    this.Context.Add(order);
                }
                this.Context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var order = this.Context.Orders.Find(id);
                this.Context.Remove(order);
                this.Context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
        [HttpGet("WithoutPaging")]
        public IActionResult Get([FromQuery] OrderFilter orderFilter)
        {
            try
            {
                var orderIQ = this.Context.Orders
                .AsQueryable();
                if (orderFilter.CountryId != null)
                {
                    orderIQ = orderIQ.Where(c => c.CountryId == orderFilter.CountryId);
                }
                if (orderFilter.Code != string.Empty && orderFilter.Code != null)
                {
                    orderIQ = orderIQ.Where(c => c.Code.StartsWith(orderFilter.Code));
                }
                if (orderFilter.ClientId != null)
                {
                    orderIQ = orderIQ.Where(c => c.ClientId == orderFilter.ClientId);
                }
                if (orderFilter.RegionId != null)
                {
                    orderIQ = orderIQ.Where(c => c.RegionId == orderFilter.RegionId);
                }
                if (orderFilter.RecipientName != string.Empty && orderFilter.RecipientName != null)
                {
                    orderIQ = orderIQ.Where(c => c.RecipientName.StartsWith(orderFilter.RecipientName));
                }
                if (orderFilter.MonePlacedId != null)
                {
                    orderIQ = orderIQ.Where(c => c.MoenyPlacedId == orderFilter.MonePlacedId);
                }
                if (orderFilter.OrderplacedId != null)
                {
                    orderIQ = orderIQ.Where(c => c.OrderplacedId == orderFilter.OrderplacedId);
                }
                if (orderFilter.Phone != string.Empty && orderFilter.Phone != null)
                {
                    orderIQ = orderIQ.Where(c => c.RecipientPhones.Contains(orderFilter.Phone));
                }
                if (orderFilter.AgentId != null)
                {
                    orderIQ = orderIQ.Where(c => c.AgentId == orderFilter.AgentId);
                }
                if (orderFilter.IsClientDiliverdMoney != null)
                {
                    orderIQ = orderIQ.Where(c => c.IsClientDiliverdMoney == orderFilter.IsClientDiliverdMoney);
                }
                if (orderFilter.ClientPrintNumber != null)
                {
                    orderIQ = orderIQ.Where(c => c.OrderPrints.Any(op => op.Print.PrintNmber == orderFilter.ClientPrintNumber && op.Print.Type == PrintType.Client));
                }
                if (orderFilter.AgentPrintNumber != null)
                {
                    orderIQ = orderIQ.Where(c => c.OrderPrints.Any(op => op.Print.PrintNmber == orderFilter.AgentPrintNumber && op.Print.Type == PrintType.Agent));
                }
                if (orderFilter.CreatedDate != null)
                {
                    orderIQ = orderIQ.Where(c => c.Date == orderFilter.CreatedDate);
                }
                var total = orderIQ.Count();
                var orders = orderIQ
                    .Include(c => c.Client)
                        .ThenInclude(c => c.ClientPhones)
                    .Include(c => c.Agent)
                        .ThenInclude(c => c.UserPhones)
                    .Include(c => c.Region)
                    .Include(c => c.Country)
                    .Include(c => c.Orderplaced)
                    .Include(c => c.MoenyPlaced)
                    .Include(c => c.OrderItems)
                        .ThenInclude(c => c.OrderTpye)
                    .Include(c => c.OrderPrints)
                        .ThenInclude(c => c.Print)
                    .ToList();
                return Ok(new { data = mapper.Map<OrderDto[]>(orders), total });
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
        [HttpGet]
        public IActionResult Get([FromQuery] PagingDto pagingDto, [FromQuery] OrderFilter orderFilter)
        {
            try
            {
                var orderIQ = this.Context.Orders
               .AsQueryable();
                if (orderFilter.CountryId != null)
                {
                    orderIQ = orderIQ.Where(c => c.CountryId == orderFilter.CountryId);
                }
                if (orderFilter.Code != string.Empty && orderFilter.Code != null)
                {
                    orderIQ = orderIQ.Where(c => c.Code.StartsWith(orderFilter.Code));
                }
                if (orderFilter.ClientId != null)
                {
                    orderIQ = orderIQ.Where(c => c.ClientId == orderFilter.ClientId);
                }
                if (orderFilter.RegionId != null)
                {
                    orderIQ = orderIQ.Where(c => c.RegionId == orderFilter.RegionId);
                }
                if (orderFilter.RecipientName != string.Empty && orderFilter.RecipientName != null)
                {
                    orderIQ = orderIQ.Where(c => c.RecipientName.StartsWith(orderFilter.RecipientName));
                }
                if (orderFilter.MonePlacedId != null)
                {
                    orderIQ = orderIQ.Where(c => c.MoenyPlacedId == orderFilter.MonePlacedId);
                }
                if (orderFilter.OrderplacedId != null)
                {
                    orderIQ = orderIQ.Where(c => c.OrderplacedId == orderFilter.OrderplacedId);
                }
                if (orderFilter.Phone != string.Empty && orderFilter.Phone != null)
                {
                    orderIQ = orderIQ.Where(c => c.RecipientPhones.Contains(orderFilter.Phone));
                }
                if (orderFilter.AgentId != null)
                {
                    orderIQ = orderIQ.Where(c => c.AgentId == orderFilter.AgentId);
                }
                if (orderFilter.IsClientDiliverdMoney != null)
                {
                    orderIQ = orderIQ.Where(c => c.IsClientDiliverdMoney == orderFilter.IsClientDiliverdMoney);
                }
                if (orderFilter.ClientPrintNumber != null)
                {
                    orderIQ = orderIQ.Where(c => c.OrderPrints.Any(op => op.Print.PrintNmber == orderFilter.ClientPrintNumber && op.Print.Type == PrintType.Client));
                }
                if (orderFilter.AgentPrintNumber != null)
                {
                    orderIQ = orderIQ.Where(c => c.OrderPrints.Any(op => op.Print.PrintNmber == orderFilter.AgentPrintNumber && op.Print.Type == PrintType.Agent));
                }
                if (orderFilter.CreatedDate != null)
                {
                    orderIQ = orderIQ.Where(c => c.Date == orderFilter.CreatedDate);
                }
                if (orderFilter.Note != "" && orderFilter.Note != null)
                {
                    orderIQ = orderIQ.Where(c => c.Note.Contains(orderFilter.Note));
                }
                var total = orderIQ.Count();
                var orders = orderIQ.Skip((pagingDto.Page - 1) * pagingDto.RowCount).Take(pagingDto.RowCount)
                    .Include(c => c.Client)
                    .Include(c => c.Agent)
                    .Include(c => c.Region)
                    .Include(c => c.Country)
                    .Include(c => c.Orderplaced)
                    .Include(c => c.MoenyPlaced)
                    .Include(c => c.OrderPrints)
                        .ThenInclude(c => c.Print)
                    .ToList();
                return Ok(new { data = mapper.Map<OrderDto[]>(orders), total });
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var order = this.Context.Orders
               .Include(c => c.Client)
                       .ThenInclude(c => c.ClientPhones)
                   .Include(c => c.Agent)
                       .ThenInclude(c => c.UserPhones)
                   .Include(c => c.Region)
                   .Include(c => c.Country)
                   .Include(c => c.Orderplaced)
                   .Include(c => c.MoenyPlaced)
                   .Include(c => c.OrderItems)
                       .ThenInclude(c => c.OrderTpye)
                   .Include(c => c.OrderPrints)
                       .ThenInclude(c => c.Print)
                   .Include(c => c.OrderLogs)
               .FirstOrDefault(c => c.Id == id);
            return Ok(mapper.Map<OrderDto>(order));
        }
        [HttpGet("OrdersDontFinished")]
        public IActionResult Get([FromQuery] OrderDontFinishedFilter orderDontFinishedFilter)
        {
            List<Order> orders = new List<Order>();
            if (orderDontFinishedFilter.ClientDoNotDeleviredMoney)
            {
                var list = this.Context.Orders.Where(c => c.IsClientDiliverdMoney == false && c.ClientId == orderDontFinishedFilter.ClientId && orderDontFinishedFilter.OrderPlacedId.Contains(c.OrderplacedId))
                   .Include(c => c.Region)
                   .Include(c => c.Country)
                   .Include(c => c.MoenyPlaced)
                   .Include(c => c.Orderplaced)
                   .Include(c => c.Agent)
                   .Include(c => c.OrderPrints)
                    .ThenInclude(c => c.Print)
                   .ToList();
                if (list != null && list.Count() > 0)
                {
                    orders.AddRange(list);
                }
            }
            if (orderDontFinishedFilter.IsClientDeleviredMoney)
            {

                var list = this.Context.Orders.Where(c => c.OrderStateId == (int)OrderStateEnum.ShortageOfCash && c.ClientId == orderDontFinishedFilter.ClientId && orderDontFinishedFilter.OrderPlacedId.Contains(c.OrderplacedId))
               .Include(c => c.Region)
               .Include(c => c.Country)
               .Include(c => c.Orderplaced)
               .Include(c => c.MoenyPlaced)
               .Include(c => c.Agent)
               .Include(c => c.OrderPrints)
                    .ThenInclude(c => c.Print)
               .ToList();
                if (list != null && list.Count() > 0)
                {
                    orders.AddRange(list);
                }
            }
            var o = mapper.Map<OrderDto[]>(orders);
            return Ok(o);
        }
        [HttpGet("orderPlace")]
        public IActionResult GetOrderPalce()
        {
            return Ok(mapper.Map<NameAndIdDto[]>(this.Context.OrderPlaceds.ToList()));
        }
        [HttpGet("MoenyPlaced")]
        public IActionResult GetMoenyPlaced()
        {
            return Ok(mapper.Map<NameAndIdDto[]>(this.Context.MoenyPlaceds.ToList()));
        }
        [HttpGet("chekcCode")]
        public IActionResult CheckCode([FromQuery] string code, int clientid)
        {
            return Ok(this.Context.Orders.Where(c => c.ClientId == clientid && c.Code == code).Any());
        }
        [HttpPost("CheckMulieCode/{clientId}")]
        public IActionResult CheckMulieCode(int clientId, [FromBody] string[] codes)
        {
            List<CodeStatus> codeStatuses = new List<CodeStatus>();
            var nonAvilableCode = this.Context.Orders.Where(c => c.ClientId == clientId && codes.Contains(c.Code)).Select(c => c.Code).ToArray();
            codeStatuses.AddRange(codes.Except(nonAvilableCode).Select(c => new CodeStatus()
            {
                Code = c,
                Avilabe = true
            }));
            codeStatuses.AddRange(nonAvilableCode.Select(c => new CodeStatus()
            {
                Avilabe = false,
                Code = c
            }));

            return Ok(codeStatuses);
        }
        [HttpGet("NewOrders")]
        public IActionResult GetNewOrders()
        {
            var orders = this.Context.Orders
                .Include(c => c.Client)
                .Include(c => c.Agent)
                .Include(c => c.Region)
                .Include(c => c.Country)
                .Include(c => c.Orderplaced)
                .Include(c => c.MoenyPlaced)
                .Include(c=>c.OrderItems)
                    .ThenInclude(c=>c.OrderTpye)
                .Where(c => c.IsSend == true && c.OrderplacedId == (int)OrderplacedEnum.Client)
                .ToList();
            return Ok(mapper.Map<OrderDto[]>(orders));
        }
        [HttpPut("Accept/{id}")]
        public IActionResult Accept(int id)
        {
            var order = this.Context.Orders.Find(id);
            order.OrderplacedId = (int)OrderplacedEnum.Store;
            this.Context.Update(order);
            this.Context.SaveChanges();
            return Ok();
        }
        [HttpPut("DisAccept/{id}")]
        public IActionResult DisAccept(int id)
        {
            var order = this.Context.Orders.Find(id);
            order.Seen = false;
            this.Context.Update(order);
            this.Context.SaveChanges();
            return Ok();
        }
        [HttpPut("MakeOrderInWay")]
        public IActionResult MakeOrderInWay([FromBody] DateWithId<int> dateWithId)
        {
            var ids = dateWithId.Ids;
            var orders = this.Context.Orders
                .Include(c => c.Agent)
                .ThenInclude(c => c.UserPhones)
                .Include(c => c.Client)
                .Include(c => c.Country)
                .Where(c => ids.Contains(c.Id)).ToList();
            if (orders.Any(c => c.OrderplacedId != (int)OrderplacedEnum.Store))
            {
                return Conflict();
            }

            var oldPrint = this.Context.Printeds.Where(c => c.Type == PrintType.Agent && c.PrintNmber == this.Context.Printeds.Where(c => c.Type == PrintType.Agent).Max(c => c.PrintNmber)).FirstOrDefault();
            var printNumber = oldPrint?.PrintNmber ?? 0;
            ++printNumber;
            var agent = orders.FirstOrDefault().Agent;
            var newPrint = new Printed()
            {
                PrintNmber = printNumber,
                Date = dateWithId.Date,
                Type = PrintType.Agent,
                PrinterName = User.Claims.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault().Value,
                DestinationName = agent.Name,
                DestinationPhone = agent.UserPhones.FirstOrDefault()?.Phone ?? "",
            };
            var transaction = this.Context.Database.BeginTransaction();
            try
            {
                this.Context.Printeds.Add(newPrint);
                this.Context.SaveChanges();
                foreach (var item in orders)
                {


                    item.OrderplacedId = (int)OrderplacedEnum.Way;
                    this.Context.Update(item);
                    this.Context.Entry(item).Reference(c => c.Region).Load();
                    var orderPrint = new OrderPrint()
                    {
                        PrintId = newPrint.Id,
                        OrderId = item.Id
                    };
                    var AgentPrint = new AgnetPrint()
                    {
                        Code = item.Code,
                        ClientName = item.Client.Name,
                        Note = item.Note,
                        Total = item.Cost,
                        Country = item.Country.Name,
                        PrintId = newPrint.Id,
                        Phone = item.RecipientPhones,
                        Region = item.Region?.Name
                    };
                    this.Context.Add(orderPrint);
                    this.Context.Add(AgentPrint);
                }
                this.Context.SaveChanges();
                transaction.Commit();
                return Ok(new { printNumber });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest();
            }
        }
        /// <summary>
        /// <!--استلام حالة شحنة-->
        /// </summary>
        /// <param name="orderStates"></param>
        /// 
        /// <returns></returns>
        [HttpPut("UpdateOrdersStatusFromAgent")]
        public IActionResult UpdateOrdersStatusFromAgent(List<OrderStateDto> orderStates)
        {

            foreach (var item in orderStates)
            {
                var order = this.Context.Orders.Find(item.Id);
                OrderLog log = order;
                this.Context.Add(log);
                order.OrderplacedId = item.OrderplacedId;
                order.MoenyPlacedId = item.MoenyPlacedId;
                order.Note = item.Note;
                order.DeliveryCost = item.DeliveryCost;
                order.AgentCost = item.AgentCost;
                order.SystemNote = "UpdateOrdersStatusFromAgent";
                if (order.IsClientDiliverdMoney)
                {
                    switch (order.OrderplacedId)
                    {
                        case (int)OrderplacedEnum.Delivered:
                            {
                                order.OrderStateId = (int)OrderStateEnum.Finished;
                                if (order.Cost != item.Cost)
                                {
                                    if (order.OldCost == null)
                                        order.OldCost = order.Cost;
                                    order.Cost = item.Cost;
                                }
                            }
                            break;
                        case (int)OrderplacedEnum.CompletelyReturned:
                            {
                                if (order.OldCost == null)
                                    order.OldCost = order.Cost;
                                //order.DeliveryCost = 0;
                                order.Cost = 0;
                                order.AgentCost = 0;
                                order.OrderStateId = (int)OrderStateEnum.ShortageOfCash;
                            }
                            break;
                        case (int)OrderplacedEnum.Unacceptable:
                            {
                                if (order.OldCost == null)
                                {
                                    order.OldCost = order.Cost;
                                }
                                //order.OldCost = order.Cost;
                                //order.Cost = 0;
                                order.OrderStateId = (int)OrderStateEnum.ShortageOfCash;
                            }
                            break;
                        case (int)OrderplacedEnum.PartialReturned:
                            {
                                if (order.OldCost == null)
                                    order.OldCost = order.Cost;
                                order.Cost = item.Cost;
                                order.OrderStateId = (int)OrderStateEnum.ShortageOfCash;
                            }
                            break;
                    }
                }
                else
                {
                    switch (order.OrderplacedId)
                    {
                        case (int)OrderplacedEnum.PartialReturned:
                            {
                                if (order.OldCost == null)
                                    order.OldCost = order.Cost;
                                order.Cost = item.Cost;
                            }
                            break;
                        case (int)OrderplacedEnum.Delivered:
                            {
                                order.OrderStateId = (int)OrderStateEnum.Finished;

                                if (order.Cost != item.Cost)
                                {
                                    if (order.OldCost == null)
                                        order.OldCost = order.Cost;
                                    order.Cost = item.Cost;
                                }
                            }
                            break;
                        case (int)OrderplacedEnum.CompletelyReturned:
                            {
                                if (order.OldCost == null)
                                {
                                    order.OldCost = order.Cost;
                                }
                                order.Cost = 0;
                                if (order.OldDeliveryCost == null)
                                    order.OldDeliveryCost = order.DeliveryCost;
                                order.DeliveryCost = 0;
                                order.AgentCost = 0;
                            }
                            break;
                        case (int)OrderplacedEnum.Unacceptable:
                            {
                                if (order.OldCost == null)
                                    order.OldCost = order.Cost;
                                order.Cost = 0;
                            }
                            break;

                    }
                }
                this.Context.Update(order);
            }
            this.Context.SaveChanges();
            return Ok();
        }
        [HttpGet("GetClientprint")]
        public IActionResult GetClientprint([FromQuery] PagingDto pagingDto, [FromQuery] int? number, string clientName)
        {
            var orderPrintIq = this.Context.Printeds.Where(c => c.Type == PrintType.Client);
            if (number != null)
            {
                orderPrintIq = orderPrintIq.Where(c => c.PrintNmber == number);
            }
            if (clientName != null)
            {
                orderPrintIq = orderPrintIq.Where(c => c.DestinationName == clientName);
            }

            var total = orderPrintIq.Count();
            var orders = orderPrintIq.OrderByDescending(c => c.Date).Skip((pagingDto.Page - 1) * pagingDto.RowCount).Take(pagingDto.RowCount).ToList();
            return Ok(new { data = mapper.Map<PrintOrdersDto[]>(orders), total });
        }
        [HttpGet("GetAgentPrint")]
        public IActionResult GetAgentPrint([FromQuery] PagingDto pagingDto, [FromQuery] int? number, string agnetName)
        {
            var ordersPintIq = this.Context.Printeds.Where(c => c.Type == PrintType.Agent);
            if (number != null)
            {
                ordersPintIq = ordersPintIq.Where(c => c.PrintNmber == number);
            }
            if (agnetName != null)
            {
                ordersPintIq = ordersPintIq.Where(c => c.DestinationName == agnetName);
            }
            var total = ordersPintIq.Count();
            var orders = ordersPintIq.OrderByDescending(c => c.Date).Skip((pagingDto.Page - 1) * pagingDto.RowCount).Take(pagingDto.RowCount).ToList();
            return Ok(new { data = mapper.Map<PrintOrdersDto[]>(orders), total });
        }
        /// <summary>
        /// تسديد العميل
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPut("DeleiverMoneyForClient")]
        public IActionResult DeleiverMoneyForClient([FromBody] DateWithId<IdWithCost> dateWithId)
        {
            var orders = this.Context.Orders
            .Include(c => c.Client)
            .ThenInclude(c => c.ClientPhones)
            .Include(c => c.Country)
            .Where(c => dateWithId.Ids.Select(c => c.Id).Contains(c.Id)).ToList();
            var client = orders.FirstOrDefault().Client;
            if (orders.Any(c => c.ClientId != client.Id))
            {
                return Conflict();
            }
            var oldPrint = this.Context.Printeds.Where(c => c.Type == PrintType.Client && c.PrintNmber == this.Context.Printeds.Where(c => c.Type == PrintType.Client).Max(c => c.PrintNmber)).FirstOrDefault();
            var printNumber = oldPrint?.PrintNmber ?? 0;
            ++printNumber;
            var newPrint = new Printed()
            {
                PrintNmber = printNumber,
                Date = dateWithId.Date,
                Type = PrintType.Client,
                PrinterName = User.Claims.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault().Value,
                DestinationName = client.Name,
                DestinationPhone = client.ClientPhones.FirstOrDefault()?.Phone ?? "",
            };
            var transaction = this.Context.Database.BeginTransaction();
            try
            {
                this.Context.Printeds.Add(newPrint);
                this.Context.SaveChanges();
                //client.Total = 0;
                //this.Context.Update(client);
                if (orders.All(c => c.OrderplacedId == (int)OrderplacedEnum.CompletelyReturned || c.OrderplacedId == (int)OrderplacedEnum.Unacceptable))
                {
                    var recepits = this.Context.Receipts.Where(c => c.PrintId == null && c.ClientId == client.Id).ToList();
                    recepits.ForEach(c =>
                    {
                        c.PrintId = newPrint.Id;
                        this.Context.Update(c);
                    });

                    this.Context.SaveChanges();
                }
                foreach (var item in orders)
                {

                    item.IsClientDiliverdMoney = true;

                    // if (item.MoenyPlacedId == (int)MoneyPalcedEnum.InsideCompany || item.OrderplacedId > (int)OrderplacedEnum.Way)
                    // {
                    //     item.OrderStateId = (int)OrderStateEnum.Finished;
                    //     item.MoenyPlacedId = (int)MoneyPalcedEnum.Delivered;
                    // }
                    ///لتعديل من اجل ذمة المندوب
                    if (item.OrderplacedId > (int)OrderplacedEnum.Way)
                    {
                        item.OrderStateId = (int)OrderStateEnum.Finished;
                        if (item.MoenyPlacedId == (int)MoneyPalcedEnum.InsideCompany)
                            item.MoenyPlacedId = (int)MoneyPalcedEnum.Delivered;
                        //if (item.MoenyPlacedId == (int)MoneyPalcedEnum.WithAgent)
                        //    item.OrderStateId = (int)OrderStateEnum.Finished;
                    }
                    this.Context.Update(item);
                    var orderPrint = new OrderPrint()
                    {
                        PrintId = newPrint.Id,
                        OrderId = item.Id
                    };

                    var clientPrint = new ClientPrint()
                    {
                        Code = item.Code,
                        Total = item.Cost,
                        Country = item.Country.Name,
                        PrintId = newPrint.Id,
                        Phone = item.RecipientPhones,
                        DeliveCost = item.DeliveryCost,
                        Date = item.Date,
                        LastTotal = item.OldCost,
                        Note = item.Note,
                        MoneyPlacedId = item.MoenyPlacedId,
                        OrderPlacedId = item.OrderplacedId,
                        PayForClient = dateWithId.Ids.First(c => c.Id == item.Id).Cost
                    };
                    this.Context.Add(orderPrint);
                    this.Context.Add(clientPrint);
                }
                this.Context.SaveChanges();
                transaction.Commit();
                return Ok(new { printNumber });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest();

            }
         }
        /// <summary>
        /// تسليم الشركات
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPut("DeleiverMoneyForClientWithStatus")]
        public IActionResult DeleiverMoneyForClientWithStatus(DateIdCost dateIdCost)
        {
            var idCosts = dateIdCost.IdCosts;
            var ids = idCosts.Select(c => c.Id).ToList();
            var orders = this.Context.Orders
                .Include(c => c.Client)
                .ThenInclude(c => c.ClientPhones)
                .Include(c => c.Country)
                .Where(c => ids.Contains(c.Id)).ToList();
            var client = orders.FirstOrDefault().Client;
            if (orders.Any(c => c.ClientId != client.Id))
            {
                return Conflict();
            }
            var oldPrint = this.Context.Printeds.Where(c => c.Type == PrintType.Client && c.PrintNmber == this.Context.Printeds.Where(c => c.Type == PrintType.Client).Max(c => c.PrintNmber)).FirstOrDefault();
            var printNumber = oldPrint?.PrintNmber ?? 0;
            ++printNumber;
            var newPrint = new Printed()
            {
                PrintNmber = printNumber,
                Date = dateIdCost.Date,
                Type = PrintType.Client,
                PrinterName = User.Claims.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault().Value,
                DestinationName = client.Name,
                DestinationPhone = client.ClientPhones.FirstOrDefault()?.Phone ?? "",
            };
            var transaction = this.Context.Database.BeginTransaction();
            try
            {
                this.Context.Printeds.Add(newPrint);
                this.Context.SaveChanges();
                //client.Total = 0;
                //this.Context.Update(client);
                //var recepits = this.Context.Receipts.Where(c => c.PrintId == null && c.ClientId == client.Id).ToList();
                //recepits.ForEach(c =>
                //{
                //    c.PrintId = newPrint.Id;
                //    this.Context.Update(c);
                //});
                this.Context.SaveChanges();
                foreach (var item in orders)
                {

                    item.IsClientDiliverdMoney = true;
                    var newCost = idCosts.Find(c => c.Id == item.Id);
                    if (item.OldCost != null)
                    {
                        if (item.Cost != newCost.Cost)
                        {
                            item.OldCost = item.Cost;
                            item.Cost = newCost.Cost;
                        }
                    }
                    if (item.MoenyPlacedId == (int)MoneyPalcedEnum.InsideCompany || item.OrderplacedId > (int)OrderplacedEnum.Way)
                    {
                        item.OrderStateId = (int)OrderStateEnum.Finished;
                        item.MoenyPlacedId = (int)MoneyPalcedEnum.Delivered;
                    }
                    this.Context.Update(item);
                    var orderPrint = new OrderPrint()
                    {
                        PrintId = newPrint.Id,
                        OrderId = item.Id
                    };

                    var clientPrint = new ClientPrint()
                    {
                        Code = item.Code,
                        Total = item.Cost,
                        Country = item.Country.Name,
                        PrintId = newPrint.Id,
                        Phone = item.RecipientPhones,
                        DeliveCost = item.DeliveryCost,
                        MoneyPlacedId = item.MoenyPlacedId,
                        OrderPlacedId = item.OrderplacedId,
                        LastTotal = item.OldCost,
                        PayForClient = dateIdCost.IdCosts.Single(c => c.Id == item.Id).PayForClient,
                        Date = item.Date,
                        Note = item.Note
                    };
                    this.Context.Add(orderPrint);
                    this.Context.Add(clientPrint);
                }
                this.Context.SaveChanges();

                transaction.Commit();
                return Ok(new { printNumber });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest();

            }
        }
        [HttpGet("GetOrderByAgent/{orderCode}")]
        public IActionResult GetOrderByAgent(string orderCode)
        {
            //var order = this.Context.Orders.Where(c => c.Code == orderCode).SingleOrDefault();
            //if (order == null)
            //    return Conflict(new { message = "الشحنة غير موجودة" });
            //if ((order.MoenyPlacedId > (int)MoneyPalcedEnum.WithAgent))
            //{
            //    return Conflict(new { message = "تم إستلام الشحنة مسبقاً" });
            //}
            //if (order.OrderplacedId == (int)OrderplacedEnum.CompletelyReturned || order.OrderplacedId == (int)OrderplacedEnum.Unacceptable)
            //{
            //    return Conflict(new { message = "تم إستلام الشحنة مسبقاً" });
            //}
            //if (order.OrderplacedId == (int)OrderplacedEnum.Store)
            //{
            //    return Conflict(new { message = "الشحنة ما زالت في المخزن" });
            //}
            //this.Context.Entry(order).Reference(c => c.Orderplaced).Load();
            //this.Context.Entry(order).Reference(c => c.MoenyPlaced).Load();
            //this.Context.Entry(order).Reference(c => c.Region).Load();
            //this.Context.Entry(order).Reference(c => c.Country).Load();
            var orders = this.Context.Orders.Where(c => c.Code == orderCode)
                .Include(c => c.Orderplaced)
                .Include(c => c.MoenyPlaced)
                .Include(c => c.Region)
                .Include(c => c.Country)
                .Include(c => c.Client)
                .Include(c => c.Agent)
                .ToList();
            if (orders.Count() == 0)
            {
                return Conflict(new { message = "الشحنة غير موجودة" });
            }
            var lastOrderAdded = orders.Last();
            var orderInStor = orders.Where(c => c.OrderplacedId == (int)OrderplacedEnum.Store).ToList();
            orders = orders.Except(orderInStor).ToList();

            //var fOrder = orders.Where(c => (c.MoenyPlacedId > (int)MoneyPalcedEnum.WithAgent) || c.OrderplacedId == (int)OrderplacedEnum.CompletelyReturned || c.OrderplacedId == (int)OrderplacedEnum.Unacceptable);
            var fOrder = orders.Where(c => c.OrderplacedId == (int)OrderplacedEnum.CompletelyReturned || c.OrderplacedId == (int)OrderplacedEnum.Unacceptable || (c.OrderplacedId == (int)OrderplacedEnum.Delivered && (c.MoenyPlacedId == (int)MoneyPalcedEnum.InsideCompany || c.MoenyPlacedId == (int)MoneyPalcedEnum.Delivered))).ToList();
            orders = orders.Except(fOrder).ToList();
            var orderInCompany = orders.Where(c => c.MoenyPlacedId == (int)MoneyPalcedEnum.InsideCompany).ToList();
            orders = orders.Except(orderInCompany).ToList();
            if (orders.Count() == 0)
            {
                if (lastOrderAdded.OrderplacedId == (int)OrderplacedEnum.Store)
                {
                    return Conflict(new { message = "الشحنة ما زالت في المخزن" });
                }
                //if (lastOrderAdded.OrderplacedId == (int)OrderplacedEnum.CompletelyReturned || lastOrderAdded.OrderplacedId == (int)OrderplacedEnum.Unacceptable)
                //{
                //    return Conflict(new { message = "تم إستلام الشحنة مسبقاً" });
                //}
                if (lastOrderAdded.MoenyPlacedId == (int)MoneyPalcedEnum.InsideCompany)
                {
                    return Conflict(new { message = "الشحنة داخل الشركة" });
                }
                else
                {
                    return Conflict(new { message = "تم إستلام الشحنة مسبقاً" });
                }

            }

            return Ok(mapper.Map<OrderDto[]>(orders));
        }
        [HttpGet("GetEarnings")]
        public IActionResult GetEarnings([FromQuery] PagingDto pagingDto, [FromQuery] DateFiter dateFiter)
        {
            var ordersQuery = this.Context.Orders
                .Include(c => c.Orderplaced)
                .Include(c => c.MoenyPlaced)
                .Where(c => c.OrderStateId == (int)OrderStateEnum.Finished && c.OrderplacedId != (int)OrderplacedEnum.CompletelyReturned);
            if (dateFiter.FromDate != null)
                ordersQuery = ordersQuery.Where(c => c.Date >= dateFiter.FromDate);
            if (dateFiter.ToDate != null)
                ordersQuery = ordersQuery.Where(c => c.Date <= dateFiter.ToDate);
            var totalRecord = ordersQuery.Count();
            var totalEarinig = ordersQuery.Sum(c => c.DeliveryCost - c.AgentCost);
            var orders = ordersQuery.Skip((pagingDto.Page - 1) * pagingDto.RowCount).Take(pagingDto.RowCount).ToList();
            return Ok(new { data = new { orders = mapper.Map<OrderDto[]>(orders), totalEarinig }, total = totalRecord });
        }
        [HttpGet("ShipmentsNotReimbursedToTheClient/{clientId}")]
        public IActionResult ShipmentsNotReimbursedToTheClient(int clientId)
        {
            var orders = this.Context.Orders.Where(c => c.ClientId == clientId && c.IsClientDiliverdMoney == false && c.OrderplacedId >= (int)OrderplacedEnum.Way)
                 .Include(c => c.Agent)
                     .ThenInclude(c => c.UserPhones)
                 .Include(c => c.Region)
                 .Include(c => c.Country)
                 .Include(c => c.Orderplaced)
                 .Include(c => c.MoenyPlaced).ToList();
            return Ok(mapper.Map<OrderDto[]>(orders));
        }
        [HttpPut("ReiveMoneyFromClient")]
        public IActionResult ReiveMoneyFromClient([FromBody] int[] ids)
        {
            var orders = this.Context.Orders.Where(c => ids.Contains(c.Id))
                .ToList();
            orders.ForEach(c => c.OrderStateId = (int)OrderStateEnum.Finished);
            this.Context.SaveChanges();
            return Ok();
        }
        [HttpGet("GetOrderByAgnetPrintNumber")]
        public IActionResult GetOrderByAgnetPrintNumber([FromQuery] int printNumber)
        {
            var printed = this.Context.Printeds.Where(c => c.PrintNmber == printNumber && c.Type == PrintType.Agent)
                .Include(c => c.AgnetPrints)
                .FirstOrDefault();
            if (printed == null)
                return Conflict();
            var x = mapper.Map<PrintOrdersDto>(printed);
            return Ok(x);
        }
        [HttpGet("GetOrderByClientPrintNumber")]
        public IActionResult GetOrderByClientPrintNumber([FromQuery] int printNumber)
        {
            var printed = this.Context.Printeds.Where(c => c.PrintNmber == printNumber && c.Type == PrintType.Client)
                .Include(c => c.ClientPrints)
                .Include(c => c.ClientPrints)
                    .ThenInclude(c => c.OrderPlaced)
                .FirstOrDefault();
            if (printed == null)
                return Conflict();
            var x = mapper.Map<PrintOrdersDto>(printed);
            return Ok(x);
        }
        /// <summary>
        /// طلبات في ذمة المندوب
        /// </summary>
        /// <param name="agnetId"></param>
        /// <returns></returns>
        [HttpGet("OrderVicdanAgent/{agnetId}")]
        public IActionResult OrderVicdanAgent(int agnetId)
        {
            var orders = this.Context.Orders.
                Where(c => c.AgentId == agnetId)
                .Where(c => (c.MoenyPlacedId == (int)MoneyPalcedEnum.WithAgent) || (c.IsClientDiliverdMoney == true && c.OrderplacedId == (int)OrderplacedEnum.Way))
                .Include(c => c.Client)
                 .Include(c => c.Region)
                 .Include(c => c.Country)
                 .Include(c => c.Orderplaced)
                 .Include(c => c.MoenyPlaced);
            return Ok(mapper.Map<OrderDto[]>(orders));
        }
        [HttpGet("OrderInCompany/{clientId}/{code}")]
        public IActionResult OrderInCompany(int clientId, string code)
        {
            var order = this.Context.Orders.Where(c => c.ClientId == clientId && c.Code == code)
                .Include(c => c.MoenyPlaced)
                .Include(c => c.Orderplaced)
                .Include(c => c.Country)
                .ThenInclude(c => c.Regions)
                .FirstOrDefault();

            if (order == null)
            {
                return Conflict(new { Message = "الشحنة غير موجودة" });
            }

            if (order.IsClientDiliverdMoney && order.OrderStateId != (int)OrderStateEnum.ShortageOfCash)
            {
                return Conflict(new { Message = "تم تسليم كلفة الشحنة من قبل" });
            }
            //if (order.OrderplacedId < (int)OrderplacedEnum.Delivered)
            //{
            //    return Conflict(new { Message = "لم يتم إستلام حالة الشحنة مسبقاً" });
            //}
            if (order.MoenyPlacedId != (int)MoneyPalcedEnum.InsideCompany)
            {
                return Conflict(new { Message = "الشحنة ليست داخل الشركة" });
            }
            return Ok(mapper.Map<OrderDto>(order));
        }
        [HttpPut("ReSend")]
        public IActionResult ReSend([FromBody] OrderReSend orderReSend)
        {
            var order = this.Context.Orders.Find(orderReSend.Id);
            order.CountryId = orderReSend.CountryId;
            order.RegionId = orderReSend.RegionId;
            order.AgentId = orderReSend.AgnetId;
            if (order.OldCost != null)
            {
                order.Cost = (decimal)order.OldCost;
                order.OldCost = null;
            }

            order.OrderStateId = (int)OrderStateEnum.Processing;
            order.OrderplacedId = (int)OrderplacedEnum.Store;
            order.DeliveryCost = orderReSend.DeliveryCost;
            order.MoenyPlacedId = (int)MoneyPalcedEnum.OutSideCompany;
            order.AgentCost = this.Context.Users.Find(order.AgentId).Salary ?? 0;
            this.Context.Update(order);
            this.Context.SaveChanges();
            return Ok();
        }
        [HttpPut("MakeStoreOrderCompletelyReturned")]
        public IActionResult MakeOrderCompletelyReturned([FromBody] int id)
        {
            var order = this.Context.Orders.Find(id);
            OrderLog log = order;
            this.Context.Add(log);
            if (order.OrderplacedId != (int)OrderplacedEnum.Store)
            {
                return Conflict();
            }
            order.OrderplacedId = (int)OrderplacedEnum.CompletelyReturned;
            order.MoenyPlacedId = (int)MoneyPalcedEnum.InsideCompany;
            order.OldCost = order.Cost;
            order.Cost = 0;
            if (order.OldDeliveryCost == null)
                order.OldDeliveryCost = order.DeliveryCost;
            order.DeliveryCost = 0;
            order.AgentCost = 0;
            order.UpdatedDate = DateTime.Now;
            order.UpdatedBy = AuthoticateUserName();
            order.SystemNote = "MakeStoreOrderCompletelyReturned";
            this.Context.Update(order);
            OrderLog orderLog = new OrderLog();
            this.Context.SaveChanges();
            return Ok(mapper.Map<OrderDto>(order));
        }

    }
}
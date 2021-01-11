﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using KokazGoodsTransfer.Dtos.Common;
using KokazGoodsTransfer.Dtos.IncomesDtos;
using KokazGoodsTransfer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KokazGoodsTransfer.Controllers.EmployeePolicyControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncomeController : AbstractEmployeePolicyController
    {
        public IncomeController(KokazContext context, IMapper mapper) : base(context, mapper)
        {
        }
        [HttpGet]
        public IActionResult Get([FromQuery]Filtering filtering)
        {
            try
            {
                var incomeIQ = (IQueryable<Income>)this.Context.Incomes
                       .Include(c => c.User)
                       .Include(c => c.IncomeType)
                       .Include(c => c.Currency);
                if (filtering.MaxAmount != null)
                    incomeIQ = incomeIQ.Where(c => c.Amount <= filtering.MaxAmount);
                if (filtering.MinAmount != null)
                    incomeIQ = incomeIQ.Where(c => c.Amount >= filtering.MaxAmount);
                if (filtering.Type != null)
                    incomeIQ = incomeIQ.Where(c => c.IncomeTypeId == filtering.Type);
                if (filtering.UserId != null)
                    incomeIQ = incomeIQ.Where(c => c.UserId == filtering.UserId);
                if (filtering.FromDate != null)
                    incomeIQ = incomeIQ.Where(c => c.Date >= filtering.FromDate);
                if (filtering.ToDate != null)
                    incomeIQ = incomeIQ.Where(c => c.Date <= filtering.ToDate);
                if (filtering.CurrencyId != null)
                    incomeIQ = incomeIQ.Where(c => c.CurrencyId == filtering.CurrencyId);
                return Ok(mapper.Map<IncomeDto[]>(incomeIQ.ToList()));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpPost]
        public IActionResult Create([FromBody] CreateIncomeDto creatrIncomeDto)
        {
            try
            {
                var income = mapper.Map<Income>(creatrIncomeDto);
                income.UserId = AuthoticateUserId();
                this.Context.Add(income);
                this.Context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
        [HttpPost("AddMultiple")]
        public IActionResult Create([FromBody]IList<CreateIncomeDto> createIncomeDtos)
        {
            try
            {
                var userId = AuthoticateUserId();
                foreach (var item in createIncomeDtos)
                {
                    var incmoe = mapper.Map<Income>(item);
                    incmoe.UserId = userId;
                    this.Context.Add(incmoe);
                }
                this.Context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
        //[HttpPatch]
        //public IActionResult Update([FromBody])
    }
}
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class DebitCardController : GenericController<DebitCard>
    {
        private readonly IRepository<DebitCard> _repository;

        public DebitCardController(IRepository<DebitCard> repository) : base(repository)
        {
            _repository = repository;
        }
    }
}
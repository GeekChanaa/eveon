using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using AutoMapper;

namespace VoltaXApi.Data
{
    public class CommentRepository : Repository<Comment>,ICommentRepository
    {
        private readonly IMapper _mapper;
        public CommentRepository(
            VoltaXApiDbContext context,
            IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task CreateComment(CreateCommentDto comment)
        {
            Comment comm = _mapper.Map<CreateCommentDto,Comment>(comment);
            await base.AddAsync(comm);
        }

        public async Task<CommentDisplayDto> GetCommentByID(int commentID)
        {
            Comment comm = await _context.Comments
                .Include(u => u.User)
                .Include(u => u.ChargingStation)
                .Include(u => u.ChargePoint)
                .Include(u => u.Connector).Where(u => u.ID == commentID).FirstOrDefaultAsync();
            var comment = _mapper.Map<Comment,CommentDisplayDto>(comm);
            return comment;
        }
    }
}


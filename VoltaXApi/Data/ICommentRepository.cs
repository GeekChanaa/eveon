using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public interface ICommentRepository : IRepository<Comment>
    {
        Task CreateComment(CreateCommentDto comment);
        Task<CommentDisplayDto> GetCommentByID(int commentID);

    }
}
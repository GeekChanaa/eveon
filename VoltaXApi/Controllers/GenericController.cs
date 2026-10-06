using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using VoltaXApi.Helpers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class GenericController<T> : ControllerBase where T : class, IEntity
    {
        private readonly IRepository<T> _repository;

        public GenericController(IRepository<T> repository)
        {
            _repository = repository;
        }

        // GET: api/T
        [HttpGet]
        public virtual async Task<IActionResult> GetAll([FromQuery] GlobalParams globalParams)
        {
            Type t = typeof(T);
            var classes = await PagedList<T>.CreateAsync(_repository.GetAllAsync(globalParams), globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(classes.CurrentPage, classes.PageSize, classes.TotalCount, classes.TotalPages);
            return Ok(classes);
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                return NotFound();
            }

            return Ok(entity);
        }



        // PUT: api/T/5
        // Only the scalar fields the client may change are copied onto the stored entity
        // (see EntityUpdate); ids, ownership, balances, credentials and audit fields are ignored.
        [HttpPut("{id}")]
        public virtual async Task<IActionResult> Update(int id, [FromBody] JsonElement entityToUpdate)
        {
            if (entityToUpdate.ValueKind != JsonValueKind.Object)
                return BadRequest("Expected a JSON object.");
            if (TryGetId(entityToUpdate, out var bodyId) && bodyId != id)
                return BadRequest("The id in the body does not match the URL.");

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return NotFound();

            var jsonOptions = HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
            EntityUpdate.Apply(entity, entityToUpdate, jsonOptions);
            await _repository.Update(entity);
            return NoContent();
        }

        private static bool TryGetId(JsonElement body, out int id)
        {
            id = 0;
            foreach (var field in body.EnumerateObject())
                if (string.Equals(field.Name, "id", StringComparison.OrdinalIgnoreCase))
                    return field.Value.ValueKind == JsonValueKind.Number && field.Value.TryGetInt32(out id);
            return false;
        }

        // POST: api/T
        [HttpPost]
        public virtual async Task<IActionResult> Create(T entity)
        {
            if (entity == null)
            {
                return BadRequest("Entity is null");
            }

            var cleanEntity = (T)Activator.CreateInstance(typeof(T))!;
            foreach (var property in typeof(T).GetProperties().Where(p => p.CanRead && p.CanWrite && (p.PropertyType.IsValueType || p.PropertyType == typeof(string))))
                if (property.Name is not "ID" and not "IsDeleted" and not "CreatedAt" and not "UpdatedAt" && !EntityUpdate.IsCredential(property.Name)) property.SetValue(cleanEntity, property.GetValue(entity));
            entity = cleanEntity;
            await _repository.AddAsync(entity);
            return CreatedAtAction("GetById", new { id = entity.ID }, entity);
        }

        // DELETE: api/T/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();
            await _repository.Remove(item);
            return NoContent();
        }

        [HttpGet("countAll")]
        public async Task<IActionResult> CountAll()
        {
            int count = await _repository.CountAsync(u => true);
            return Ok(count);
        }
        protected async Task<IActionResult> OkWithPagination<K>(PagedList<K> entities)
        {
            Response.AddPagination(entities.CurrentPage, entities.PageSize, entities.TotalCount, entities.TotalPages);
            return Ok(entities);
        }
    }

}
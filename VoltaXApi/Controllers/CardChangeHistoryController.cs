using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;

namespace VoltaXApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CardChangeHistoryController(VoltaXApiDbContext db) : ControllerBase
{
    [HttpGet("card/{cardID:int}")]
    public async Task<IActionResult> GetForCard(int cardID, [FromQuery] GlobalParams globalParams)
    {
        if (!await db.Cards.IgnoreQueryFilters().AsNoTracking().AnyAsync(card => card.ID == cardID))
            return NotFound(new { error = "Charging card not found." });

        var query = db.CardChangeHistories
            .AsNoTracking()
            .Where(history => history.CardID == cardID)
            .OrderByDescending(history => history.ChangedAtUtc)
            .ThenByDescending(history => history.ID)
            .Select(history => new CardChangeHistoryDto
            {
                ID = history.ID,
                ChangeSetID = history.ChangeSetID,
                CardID = history.CardID,
                ChangedByUserID = history.ChangedByUserID,
                ChangedBy = history.ChangedByName,
                Source = history.Source,
                PropertyName = history.PropertyName,
                OldValue = history.OldValue,
                NewValue = history.NewValue,
                ChangedAtUtc = history.ChangedAtUtc
            });

        var history = await PagedList<CardChangeHistoryDto>.CreateAsync(
            query, globalParams.PageNumber, globalParams.PageSize);
        Response.AddPagination(history.CurrentPage, history.PageSize, history.TotalCount, history.TotalPages);
        return Ok(history);
    }
}

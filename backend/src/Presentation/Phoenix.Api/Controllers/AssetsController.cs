using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Phoenix.Domain.Entities;
using Phoenix.Infrastructure.Data;

namespace Phoenix.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AssetsController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Asset>>> GetAssets()
    {
        var userId = GetCurrentUserId();
        return await _context.Assets
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Asset>> PostAsset(Asset asset)
    {
        asset.UserId = GetCurrentUserId();
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAssets), new { id = asset.Id }, asset);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAsset(int id, [FromBody] UpdateAssetRequest request)
    {
        var userId = GetCurrentUserId();
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (asset == null)
        {
            return NotFound(new { message = "Asset not found." });
        }

        asset.Symbol = request.Symbol.ToUpper();
        asset.Name = request.Name;
        asset.Price = request.Price;
        asset.Quantity = request.Quantity;
        asset.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(asset);
    }

    [HttpPatch("{id}/price")]
    public async Task<IActionResult> PatchAssetPrice(int id, [FromBody] PatchPriceRequest request)
    {
        var userId = GetCurrentUserId();
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (asset == null)
        {
            return NotFound(new { message = "Asset not found." });
        }

        asset.Price = request.Price;
        asset.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(asset);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsset(int id)
    {
        var userId = GetCurrentUserId();
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (asset == null)
        {
            return NotFound(new { message = "Asset not found." });
        }

        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record PatchPriceRequest(decimal Price);
public record UpdateAssetRequest(string Symbol, string Name, decimal Price, decimal Quantity);

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Services;
using NotificationHandler = NotificationService.Services.NotificationService;

namespace NotificationService.Controllers;

//api/notifications
[ApiController]
[Authorize] 
[Route("api/notifications")]
public class NotificationController(NotificationHandler service) : ControllerBase
{
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub"), out var id) && id != Guid.Empty ? id : null;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (UserId is not Guid user) return Unauthorized();
        if (page < 1 || pageSize < 1 || pageSize > 100 || page > int.MaxValue / pageSize)
            return BadRequest("Page must be positive and pageSize must be between 1 and 100.");
        return Ok(await service.ListAsync(user, page, pageSize, ct));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> Unread(CancellationToken ct)
    {
        if (UserId is not Guid user) return Unauthorized();
        return Ok(new { unreadCount = await service.UnreadAsync(user, ct) });
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> Read(string id, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var notificationId))
            return BadRequest(new
            {
                message = "Notification ID must be a valid GUID."
            });

        if (UserId is not Guid user)
            return Unauthorized();

        var result = await service.ReadAsync(user, notificationId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        if (UserId is not Guid user) return Unauthorized();
        return Ok(await service.ReadAllAsync(user, ct));
    }
}

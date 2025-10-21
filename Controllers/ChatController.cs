using HUIT_Library.DTOs.DTO;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HUIT_Library.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [Authorize]
    [HttpPost("session/create")]
    public async Task<IActionResult> CreateSession([FromBody] CreateChatRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var session = await _chatService.CreateSessionAsync(userId, request);
        if (session == null) return BadRequest(new { message = "Không thể tạo phiên chat" });

        return Ok(new { maPhienChat = session.MaPhienChat });
    }

    [Authorize]
    [HttpPost("message/send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var msg = await _chatService.SendMessageAsync(userId, request);
        if (msg == null) return BadRequest(new { message = "Phiên chat không tồn tại" });

        return Ok(new MessageDto
        {
            MaTinNhan = msg.MaTinNhan,
            MaPhienChat = msg.MaPhienChat,
            MaNguoiGui = msg.MaNguoiGui,
            NoiDung = msg.NoiDung,
            ThoiGianGui = msg.ThoiGianGui,
            LaBot = msg.LaBot
        });
    }

    [Authorize]
    [HttpGet("messages/{maPhienChat}")]
    public async Task<IActionResult> GetMessages(int maPhienChat)
    {
        var messages = await _chatService.GetMessagesAsync(maPhienChat);
        return Ok(messages);
    }
}

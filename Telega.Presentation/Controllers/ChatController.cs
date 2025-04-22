using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Telega.Application.Services;
using System.Security.Claims;
using static Telega.Application.DTOs.DTO;

namespace Telega.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }
        // Создание нового чата
        [HttpPost]
        public async Task <IActionResult> CreateChat([FromBody] CreateChatRequestDto request)
        {
            var UserId = GetCurrentUserId();
            var chat = await _chatService.CreateChatAsync(request.Name, request.Type, UserId);
            return Ok(chat);
        }
        // Добавление пользователя в чат
        [HttpPost("{chatId}/members")]
        public async Task<IActionResult> AddUserToChat(Guid chatId, [FromBody] AddUserToChatRequestDto request)
        {
            var UserId = GetCurrentUserId();
            await _chatService.AddUserToChatAsync(chatId, request.UserId);
            return NoContent();
        }
        // Получение списка чатов пользователя
        [HttpGet]
        public async Task<IActionResult> GetUserChats()
        {
            var UserId = GetCurrentUserId();
            var chats = await _chatService.GetUserChatsAsync(UserId);
            return Ok(chats);
        }

        private Guid GetCurrentUserId()
        {
            //Console.WriteLine($"User.Identity.IsAuthenticated: {User.Identity?.IsAuthenticated}");
            //foreach (var claim in User.Claims)
            //{
            //    Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            //}
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Неверный идентификатор пользователя в токене");
            return userId;
        }
    }
}

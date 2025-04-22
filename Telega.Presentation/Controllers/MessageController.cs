using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using Telega.Application.Services;
using Telega.Domain.Entities;
using static Telega.Application.DTOs.DTO;

namespace Telega.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }
        // отправка текстового сообщения
        [HttpPost("text")]
        [SwaggerOperation(Summary = "Отправить текстовое сообщение", Description = "Отправляет текстовое сообщение в чат.")]
        [SwaggerResponse(200, "Сообщение успешно отправлено", typeof(MessageDto))]
        [SwaggerResponse(400, "Недопустимый запрос")]
        public async Task<IActionResult> SendTextMessage([FromBody] SendTextMessageRequestDto request)
        {
            var userId = GetCurrentUserId();
            TimeSpan? timeToLive = request.TimeToLive.HasValue && request.TimeToLive != TimeSpan.Zero
                ? request.TimeToLive
                : null;
            var message = await _messageService.SendMessageAsync(request.ChatId, userId, request.Content, request.ContentType, timeToLive);
            return Ok(message);
        }

        // отправка медиа-сообщения
        [HttpPost("media")]
        [SwaggerOperation(Summary = "Отправить медиа сообщение", Description = "Отправляет медиафайл в чат.")]
        [SwaggerResponse(200, "Мультимедийное сообщение отправлено успешно", typeof(MessageDto))]
        [SwaggerResponse(400, "Требуется файл")]
        public async Task<IActionResult> SendMediaMessage([FromForm] SendMediaMessageRequestDto request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Требуется файл.");

            var userId = GetCurrentUserId();
            using var stream = request.File.OpenReadStream();
            var message = await _messageService.SendMediaMessageAsync(request.ChatId, userId, stream, request.File.FileName, request.TimeToLive);
            return Ok(message);
        }
        // Получение списка сообщений чата
        [HttpGet("{chatId}")]
        [SwaggerOperation(Summary = "Получать сообщения в чате", Description = "Извлекает все сообщения в чате.")]
        [SwaggerResponse(200, "Список сообщений", typeof(IReadOnlyCollection<MessageDto>))]
        public async Task<IActionResult> GetChatMessages(Guid chatId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var (messages, totalCount) = await _messageService.GetChatMessagesAsync(chatId, page, pageSize);
            var response = new PaginatedMessagesResponseDto
            {
                Messages = messages,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
            return Ok(response);
        }
        [HttpGet("media/{messageId}")]
        [SwaggerOperation(Summary = "Скачать медиа-сообщение", Description = "Скачивает медиа-сообщение по его идентификатору.")]
        [SwaggerResponse(200, "Успещно")]
        public async Task<IActionResult> DownloadMediaMessage(Guid messageId)
        {
            var message = await _messageService.GetMessageByIdAsync(messageId);
            if (message == null)
                return NotFound("Message not found.");
            if (message.ContentType == MessageContentType.Text)
                return BadRequest("Text messages cannot be downloaded as media.");

            var (stream, fileName, contentType) = await _messageService.DownloadMediaMessageAsync(messageId);
            return File(stream, contentType, fileName);
        }
        [HttpGet("media/preview/{messageId}")]
        [SwaggerOperation(Summary = "Получить URL превью медиа-сообщения", Description = "Получает URL превью медиа-сообщения.")]
        [SwaggerResponse(200, "Успешно")]
        public async Task<IActionResult> GetMediaPreviewUrl(Guid messageId)
        {
            var url = await _messageService.GetMediaPreviewUrlAsync(messageId);
            return Ok(new { PreviewUrl = url });
        }

        [HttpPost("mark-as-read/{messageId}")]
        [SwaggerOperation(Summary = "Отметить сообщение как прочитанное", Description = "Отмечает сообщение как прочитанное.")]
        [SwaggerResponse(204, "Сообщение успешно отмечено как прочитанное")]
        public async Task<IActionResult> MarkMessageAsRead(Guid messageId)
        {
            var userId = GetCurrentUserId();
            await _messageService.MarkMessageAsReadAsync(messageId, userId);
            return NoContent();
        }
        [HttpDelete("{messageId}")]
        [SwaggerOperation(Summary = "Удалить сообщение", Description = "Удаляет сообщение и его носитель (если применимо).")]
        [SwaggerResponse(204, "Сообщение успешно удалено")]
        public async Task<IActionResult> RemoveMessage(Guid messageId)
        {
            var userId = GetCurrentUserId();
            await _messageService.RemoveMessageAsync(messageId, userId);
            return NoContent();
        }

        [HttpPost("broadcast")]
        [SwaggerOperation(Summary = "Рассылка сообщения", Description = "Рассылает сообщение всем пользователям чата.")]
        [SwaggerResponse(200, "Сообщение успешно разослано", typeof(IReadOnlyCollection<MessageDto>))]
        [SwaggerResponse(400, "Не валидный чат")]
        public async Task<IActionResult> BroadcastMessage([FromBody] BroadcastMessageRequestDto request)
        {
            var userId = GetCurrentUserId();
            TimeSpan? timeToLive = request.TimeToLive.HasValue && request.TimeToLive != TimeSpan.Zero
                ? request.TimeToLive
                : null;
            var messages = await _messageService.BroadcastMessageAsync(userId, request.ChatIds, request.Content, request.ContentType, timeToLive);
            return Ok(messages);
        }
        [HttpPost("media/multiple")]
        [SwaggerOperation(Summary = "Send multiple media messages", Description = "Sends multiple media files to a chat.")]
        [SwaggerResponse(200, "Media messages sent successfully", typeof(IReadOnlyCollection<MessageDto>))]
        [SwaggerResponse(400, "At least one file is required")]
        public async Task<IActionResult> SendMultipleMediaMessages([FromForm] SendMultipleMediaMessageRequestDto request)
        {
            if (request.Files == null || !request.Files.Any())
                return BadRequest("At least one file is required.");

            var userId = GetCurrentUserId();
            TimeSpan? timeToLive = request.TimeToLive.HasValue && request.TimeToLive != TimeSpan.Zero ? request.TimeToLive : null;
            var files = request.Files.Select(f => (f.OpenReadStream(), f.FileName));
            var messages = await _messageService.SendMultipleMediaMessagesAsync(request.ChatId, userId, files, timeToLive);
            return Ok(messages);
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

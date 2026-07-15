using System.ComponentModel.DataAnnotations;

namespace SmartGridAPI.DTOs
{
    public class ChatMessageDto
    {
        [Required]
        public string Role { get; set; } = "user"; // "user" or "model"
        
        [Required]
        public string Content { get; set; } = string.Empty;
    }

    public class ChatRequestDto
    {
        [Required]
        public string Message { get; set; } = string.Empty;

        public List<ChatMessageDto> History { get; set; } = new List<ChatMessageDto>();
    }
}

using System;
using System.Collections.Generic;

namespace DocMind.Models
{
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;       // "user" or "assistant"
        public string Content { get; set; } = string.Empty;    // message text
        public DateTime Timestamp { get; set; }
        public string Mode { get; set; } = "document";         // "general" or "document"
        public List<SourceDto> Sources { get; set; } = new();
    }
}

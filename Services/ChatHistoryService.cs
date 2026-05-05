using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using DocMind.Models;

namespace DocMind.Services
{
    public class ChatHistoryService
    {
        private static ChatHistoryService? _instance;
        public static ChatHistoryService Instance => _instance ??= new ChatHistoryService();

        private static readonly string ChatHistoryFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DocMind",
            "chat_history.json"
        );

        private ChatHistoryService() { }

        public List<ChatMessage> LoadHistory()
        {
            try
            {
                if (!File.Exists(ChatHistoryFile))
                    return new List<ChatMessage>();

                string json = File.ReadAllText(ChatHistoryFile, Encoding.UTF8);
                return JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new List<ChatMessage>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load chat history: {ex.Message}");
                return new List<ChatMessage>();
            }
        }

        public void SaveHistory(IEnumerable<ChatMessage> messages)
        {
            try
            {
                // Ensure directory exists
                string? dir = Path.GetDirectoryName(ChatHistoryFile);
                if (dir != null)
                {
                    Directory.CreateDirectory(dir);
                }

                // Only save last 100 messages to avoid the file growing too large over time
                var toSave = messages.TakeLast(100).ToList();

                string json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(ChatHistoryFile, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save chat history: {ex.Message}");
            }
        }

        public void ClearHistory()
        {
            try
            {
                if (File.Exists(ChatHistoryFile))
                    File.Delete(ChatHistoryFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clear chat history: {ex.Message}");
            }
        }
    }
}

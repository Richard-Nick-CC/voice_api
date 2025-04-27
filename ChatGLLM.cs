using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class ChatGLM
{
    private readonly HttpClient _httpClient;
    private readonly string _url;
    private readonly string _apiKey;

    public ChatGLM(string url, string apiKey)
    {
        _httpClient = new HttpClient();
        _url = url;
        _apiKey = apiKey;

        // 设置请求头
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<string> Chat(string message, string history = "")
    {
        try
        {
            Console.WriteLine("正在发送API请求...\n");

            var messages = new List<Dictionary<string, string>>();
            
            // 添加系统消息
            messages.Add(new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", "You are a helpful assistant." }
            });

            // 如果有历史记录，添加到消息列表中
            if (!string.IsNullOrEmpty(history))
            {
                var historyMessages = JsonSerializer.Deserialize<List<string[]>>(history);
                foreach (var historyMessage in historyMessages)
                {
                    messages.Add(new Dictionary<string, string>
                    {
                        { "role", "user" },
                        { "content", historyMessage[0] }
                    });
                    messages.Add(new Dictionary<string, string>
                    {
                        { "role", "assistant" },
                        { "content", historyMessage[1] }
                    });
                }
            }

            // 添加当前用户消息
            messages.Add(new Dictionary<string, string>
            {
                { "role", "user" },
                { "content", message }
            });

            var requestData = new
            {
                model = "glm-4",
                messages = messages,
                temperature = 0.9,
                top_p = 0.7
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json"
            );

            // 构建完整的API URL
            string apiUrl = _url.TrimEnd('/') + "/chat/completions";
            
            // 打印请求信息以便调试
            Console.WriteLine($"发送请求到: {apiUrl}");
            Console.WriteLine($"请求数据: {await content.ReadAsStringAsync()}\n");

            var response = await _httpClient.PostAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"服务器响应: {responseBody}");
                return $"错误: HTTP {(int)response.StatusCode} ({response.StatusCode})\n响应内容: {responseBody}";
            }

            Console.WriteLine("API 请求成功！");
            Console.WriteLine("======================================== 完整响应结构 ========================================");
            Console.WriteLine(responseBody);
            Console.WriteLine("\n======================================== 回复内容 ========================================");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<ChatResponse>(responseBody, options);
            
            if (result?.Choices != null && result.Choices.Length > 0)
            {
                var messageContent = result.Choices[0].Message.Content;
                var role = result.Choices[0].Message.Role;
                
                Console.WriteLine($"角色: {role}");
                Console.WriteLine($"内容: {messageContent}\n");
                
                return messageContent;
            }
            
            return "无响应";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析错误: {ex.Message}");
            return $"错误: {ex.Message}";
        }
    }
}

public class ChatResponse
{
    [JsonPropertyName("choices")]
    public Choice[] Choices { get; set; }
    
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("created")]
    public long Created { get; set; }
    
    [JsonPropertyName("model")]
    public string Model { get; set; }
    
    [JsonPropertyName("usage")]
    public Usage Usage { get; set; }
    
    [JsonPropertyName("request_id")]
    public string RequestId { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")]
    public Message Message { get; set; }
    
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
    
    [JsonPropertyName("index")]
    public int Index { get; set; }
}

public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; }
}

public class Usage
{
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

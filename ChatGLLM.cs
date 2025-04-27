using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Text.Unicode;

public class ChatGLM
{
    private readonly HttpClient _httpClient;
    private readonly string _url;
    private readonly string _apiKey;
    private readonly List<Tool> _tools;
    private readonly JsonSerializerOptions _jsonOptions;

    public ChatGLM(string url, string apiKey)
    {
        // 更全面的控制台编码设置
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        // 对于Windows系统，强制使用GBK编码输出
        // 这样在中文Windows环境下更容易正确显示中文
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            try
            {
                // 中文Windows环境下，控制台默认使用GBK(CP936)编码
                Console.OutputEncoding = Encoding.GetEncoding(936); // 936是简体中文GBK的代码页
                Console.InputEncoding = Encoding.GetEncoding(936);
                
                // 输出当前编码信息（调试用）
                Console.WriteLine($"[系统信息] 操作系统: {Environment.OSVersion}");
                Console.WriteLine($"[编码信息] 控制台输出编码: {Console.OutputEncoding.EncodingName}");
                Console.WriteLine($"[编码信息] 控制台输入编码: {Console.InputEncoding.EncodingName}");
                
                // 测试输出中文
                Console.WriteLine("[测试] 中文测试: 你好，世界！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置控制台编码出错: {ex.Message}");
                // 出错时尝试使用默认的UTF-8
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
            }
        }
        else
        {
            // 非Windows系统使用UTF-8
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }

        _httpClient = new HttpClient();
        _url = url;
        _apiKey = apiKey;

        // 设置请求头
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        // 配置JSON序列化选项，确保正确处理中文
        _jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        // 初始化工具列表
        _tools = new List<Tool>
        {
            new Tool
            {
                Type = "function",
                Function = new ToolFunction
                {
                    Name = "get_flight_number",
                    Description = "根据始发地、目的地和日期，查询对应日期的航班号",
                    Parameters = new ToolParameters
                    {
                        Type = "object",
                        Properties = new Dictionary<string, ParameterProperty>
                        {
                            {
                                "departure", new ParameterProperty
                                {
                                    Description = "出发地",
                                    Type = "string"
                                }
                            },
                            {
                                "destination", new ParameterProperty
                                {
                                    Description = "目的地",
                                    Type = "string"
                                }
                            },
                            {
                                "date", new ParameterProperty
                                {
                                    Description = "日期",
                                    Type = "string"
                                }
                            }
                        },
                        Required = new[] { "departure", "destination", "date" }
                    }
                }
            },
            new Tool
            {
                Type = "function",
                Function = new ToolFunction
                {
                    Name = "get_ticket_price",
                    Description = "查询某航班在某日的票价",
                    Parameters = new ToolParameters
                    {
                        Type = "object",
                        Properties = new Dictionary<string, ParameterProperty>
                        {
                            {
                                "flight_number", new ParameterProperty
                                {
                                    Description = "航班号",
                                    Type = "string"
                                }
                            },
                            {
                                "date", new ParameterProperty
                                {
                                    Description = "日期",
                                    Type = "string"
                                }
                            }
                        },
                        Required = new[] { "flight_number", "date" }
                    }
                }
            }
        };
    }

    public async Task<string> Chat(string message, string history = "")
    {
        try
        {
            // 确保中文字符正确处理
            message = ChineseEncodingHelper.EnsureUtf8Encoding(message);
            ChineseEncodingHelper.WriteLineChinese("正在发送API请求...\n");
            ChineseEncodingHelper.WriteLineChinese($"用户消息: {message}");

            // 构建消息列表
            var messages = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "role", "user" },
                    { "content", message }
                }
            };

            // 如果有历史记录，添加到消息列表中
            if (!string.IsNullOrEmpty(history))
            {
                try
                {
                    history = ChineseEncodingHelper.EnsureUtf8Encoding(history);
                    var historyMessages = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(history);
                    messages.InsertRange(0, historyMessages);
                }
                catch (Exception ex)
                {
                    ChineseEncodingHelper.WriteLineChinese($"解析历史记录时出错: {ex.Message}");
                }
            }

            // 构建请求数据
            var requestData = new Dictionary<string, object>
            {
                { "model", "glm-4" },
                { "messages", messages },
                { "tools", _tools },
                { "tool_choice", "auto" },
                { "temperature", 0.9 },
                { "top_p", 0.7 }
            };

            // 序列化为JSON
            string jsonString = JsonSerializer.Serialize(requestData, _jsonOptions);
            
            ChineseEncodingHelper.WriteLineChinese("\nJSON请求内容:");
            ChineseEncodingHelper.WriteLineChinese(jsonString);

            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            string apiUrl = _url.TrimEnd('/') + "/chat/completions";
            
            ChineseEncodingHelper.WriteLineChinese($"\n发送请求到: {apiUrl}");
            
            var response = await _httpClient.PostAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ChineseEncodingHelper.WriteLineChinese($"服务器响应: {responseBody}");
                
                // 尝试解析错误信息
                try
                {
                    var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody, _jsonOptions);
                    if (errorObj != null && errorObj.ContainsKey("error"))
                    {
                        var error = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            errorObj["error"].ToString(), _jsonOptions);
                            
                        if (error != null)
                        {
                            string errorCode = error.ContainsKey("code") ? error["code"].ToString() : "未知";
                            string errorMsg = error.ContainsKey("message") ? error["message"].ToString() : "未知错误信息";
                            
                            if (errorCode == "401")
                            {
                                return $"认证错误(401)：{errorMsg}\n\n请检查您的API密钥格式是否正确。智谱AI的密钥格式通常为 'api-key.client-key'。";
                            }
                            
                            return $"API错误(代码:{errorCode})：{errorMsg}";
                        }
                    }
                }
                catch
                {
                    // 如果无法解析错误JSON，则使用通用错误消息
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    return "服务器内部错误(500)。这可能是暂时性问题，请稍后重试。如果问题持续存在，请检查API密钥是否正确。";
                }
                return $"错误: HTTP {(int)response.StatusCode} ({response.StatusCode})\n响应内容: {responseBody}";
            }

            ChineseEncodingHelper.WriteLineChinese("\nAPI响应内容:");
            ChineseEncodingHelper.WriteLineChinese(responseBody);

            var result = JsonSerializer.Deserialize<ChatResponse>(responseBody, _jsonOptions);
            
            if (result?.Choices != null && result.Choices.Length > 0)
            {
                var choice = result.Choices[0];
                var messageContent = choice.Message.Content;
                var role = choice.Message.Role;
                
                ChineseEncodingHelper.WriteLineChinese($"\n角色: {role}");
                ChineseEncodingHelper.WriteLineChinese($"内容: {messageContent}\n");

                if (choice.Message.ToolCalls != null && choice.Message.ToolCalls.Length > 0)
                {
                    ChineseEncodingHelper.WriteLineChinese("函数调用信息：");
                    foreach (var toolCall in choice.Message.ToolCalls)
                    {
                        ChineseEncodingHelper.WriteLineChinese($"函数名称: {toolCall.Function.Name}");
                        ChineseEncodingHelper.WriteLineChinese($"参数: {toolCall.Function.Arguments}\n");
                        
                        string functionResult = await ExecuteFunction(toolCall.Function.Name, toolCall.Function.Arguments);
                        
                        return $"{messageContent}\n函数执行结果: {functionResult}";
                    }
                }
                
                return messageContent;
            }
            
            return "无响应";
        }
        catch (Exception ex)
        {
            ChineseEncodingHelper.WriteLineChinese($"错误: {ex.Message}");
            ChineseEncodingHelper.WriteLineChinese($"堆栈跟踪: {ex.StackTrace}");
            return $"发生错误: {ex.Message}";
        }
    }

    private async Task<string> ExecuteFunction(string functionName, string arguments)
    {
        try
        {
            switch (functionName)
            {
                case "get_flight_number":
                    var flightParams = JsonSerializer.Deserialize<GetFlightNumberParams>(arguments, _jsonOptions);
                    return await GetFlightNumber(flightParams);
                case "get_ticket_price":
                    var priceParams = JsonSerializer.Deserialize<GetTicketPriceParams>(arguments, _jsonOptions);
                    return await GetTicketPrice(priceParams);
                default:
                    return $"未知函数: {functionName}";
            }
        }
        catch (Exception ex)
        {
            ChineseEncodingHelper.WriteLineChinese($"函数执行错误: {ex.Message}");
            return $"函数执行错误: {ex.Message}";
        }
    }

    private async Task<string> GetFlightNumber(GetFlightNumberParams parameters)
    {
        try
        {
            // 处理日期参数
            if (string.IsNullOrEmpty(parameters.Date))
            {
                parameters.Date = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else 
            {
                // 处理自然语言日期表达式
                switch (parameters.Date.Trim())
                {
                    case "今天":
                        parameters.Date = DateTime.Now.ToString("yyyy-MM-dd");
                        break;
                    case "明天":
                        parameters.Date = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
                        break;
                    case "后天":
                        parameters.Date = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");
                        break;
                    case "大后天":
                        parameters.Date = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd");
                        break;
                    default:
                        // 尝试解析标准日期格式
                        if (!DateTime.TryParse(parameters.Date, out DateTime parsedDate))
                        {
                            return $"错误：无法识别的日期格式。您输入的是\"{parameters.Date}\"，请使用\"今天\"、\"明天\"、\"后天\"或 yyyy-MM-dd 格式，例如：{DateTime.Now:yyyy-MM-dd}";
                        }
                        parameters.Date = parsedDate.ToString("yyyy-MM-dd");
                        break;
                }
            }

            // 注意：这里返回的是模拟数据
            string result = $"[测试数据] 航班查询结果：\n" +
                   $"- 出发地：{parameters.Departure}\n" +
                   $"- 目的地：{parameters.Destination}\n" +
                   $"- 日期：{parameters.Date}\n" +
                   $"- 模拟航班号：CA1234、MU5678（这是测试用的模拟数据）";
                   
            // 确保结果中文正确编码
            return ChineseEncodingHelper.EnsureUtf8Encoding(result);
        }
        catch (Exception ex)
        {
            return $"航班查询错误: {ex.Message}";
        }
    }

    private async Task<string> GetTicketPrice(GetTicketPriceParams parameters)
    {
        try
        {
            // 处理日期参数
            if (string.IsNullOrEmpty(parameters.Date))
            {
                parameters.Date = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else 
            {
                // 处理自然语言日期表达式
                switch (parameters.Date.Trim())
                {
                    case "今天":
                        parameters.Date = DateTime.Now.ToString("yyyy-MM-dd");
                        break;
                    case "明天":
                        parameters.Date = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
                        break;
                    case "后天":
                        parameters.Date = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");
                        break;
                    case "大后天":
                        parameters.Date = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd");
                        break;
                    default:
                        // 尝试解析标准日期格式
                        if (!DateTime.TryParse(parameters.Date, out DateTime parsedDate))
                        {
                            return $"错误：无法识别的日期格式。您输入的是\"{parameters.Date}\"，请使用\"今天\"、\"明天\"、\"后天\"或 yyyy-MM-dd 格式，例如：{DateTime.Now:yyyy-MM-dd}";
                        }
                        parameters.Date = parsedDate.ToString("yyyy-MM-dd");
                        break;
                }
            }

            // 注意：这里返回的是模拟数据
            var random = new Random();
            var mockPrice = random.Next(800, 2500);
            
            string result = $"[测试数据] 票价查询结果：\n" +
                   $"- 航班号：{parameters.FlightNumber}\n" +
                   $"- 日期：{parameters.Date}\n" +
                   $"- 模拟票价：¥{mockPrice}（这是测试用的随机价格）";
                   
            // 确保结果中文正确编码
            return ChineseEncodingHelper.EnsureUtf8Encoding(result);
        }
        catch (Exception ex)
        {
            return $"票价查询错误: {ex.Message}";
        }
    }

    // 测试控制台中文输出的辅助方法
    public void TestChineseOutput()
    {
        Console.WriteLine("========== 中文输出测试 ==========");
        Console.WriteLine("当前控制台编码: " + Console.OutputEncoding.EncodingName);
        Console.WriteLine("测试中文字符: 你好，世界！中国，北京，上海，广州，深圳");
        Console.WriteLine("测试特殊符号: ●■★☆○□◆◇※→←↑↓↖↗↙↘");
        Console.WriteLine("==================================");
    }
}

// 工具相关的类
public class Tool
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("function")]
    public ToolFunction Function { get; set; }
}

public class ToolFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("parameters")]
    public ToolParameters Parameters { get; set; }
}

public class ToolParameters
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, ParameterProperty> Properties { get; set; }

    [JsonPropertyName("required")]
    public string[] Required { get; set; }
}

public class ParameterProperty
{
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}

// 函数参数类
public class GetFlightNumberParams
{
    [JsonPropertyName("departure")]
    public string Departure { get; set; }

    [JsonPropertyName("destination")]
    public string Destination { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }
}

public class GetTicketPriceParams
{
    [JsonPropertyName("flight_number")]
    public string FlightNumber { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }
}

// 响应相关的类
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

    [JsonPropertyName("tool_calls")]
    public ToolCall[] ToolCalls { get; set; }
}

public class ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("function")]
    public FunctionCall Function { get; set; }
}

public class FunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; }
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

// 添加中文编码处理辅助类
public static class ChineseEncodingHelper
{
    // 安全输出中文到控制台
    public static void WriteLineChinese(string text)
    {
        try
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // 获取当前控制台编码
                Encoding consoleEncoding = Console.OutputEncoding;
                
                // 如果控制台使用GBK编码
                if (consoleEncoding.CodePage == 936 || consoleEncoding.WebName.ToLower().Contains("gbk"))
                {
                    // 从UTF-8转换到GBK
                    byte[] utf8Bytes = Encoding.UTF8.GetBytes(text);
                    byte[] gbkBytes = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(936), utf8Bytes);
                    string gbkString = Encoding.GetEncoding(936).GetString(gbkBytes);
                    Console.WriteLine(gbkString);
                    return;
                }
            }
            
            // 默认输出
            Console.WriteLine(text);
        }
        catch (Exception ex)
        {
            // 出错时使用原始输出
            Console.WriteLine($"[编码转换错误] {ex.Message}");
            Console.WriteLine(text);
        }
    }
    
    // 确保字符串使用UTF-8编码
    public static string EnsureUtf8Encoding(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        try
        {
            // 检测可能的编码（尝试自动检测当前编码）
            byte[] bytes;
            
            // 尝试检测GB2312/GBK编码（常见的简体中文编码）
            Encoding gbk = Encoding.GetEncoding("GBK");
            
            // 先尝试以GBK解码再编码，看是否能保持一致性
            bytes = gbk.GetBytes(input);
            string gbkString = gbk.GetString(bytes);
            
            if (gbkString == input)
            {
                // 看起来是GBK编码，转换为UTF-8
                return Encoding.UTF8.GetString(Encoding.Convert(gbk, Encoding.UTF8, bytes));
            }
            
            // 如果不是GBK，尝试直接使用UTF-8
            bytes = Encoding.UTF8.GetBytes(input);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception)
        {
            return input; // 返回原始输入
        }
    }
}

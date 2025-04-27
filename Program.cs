using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // 配置ChatGLM
        string url = "https://open.bigmodel.cn/api/paas/v4/";  // 修改为基础URL
        string apiKey = "Bearer c707cc7686924318b7fded7a1f7fa296.sSzbgplK46AOpR7C";  // API密钥

        // 创建ChatGLM实例
        var chatGLM = new ChatGLM(url, apiKey);

        Console.WriteLine("欢迎使用ChatGLM聊天程序！");
        Console.WriteLine("输入 'exit' 退出程序\n");

        string history = "";  // 用于存储对话历史

        while (true)
        {
            Console.Write("您: ");
            string input = Console.ReadLine();

            if (input.ToLower() == "exit")
                break;

            try
            {
                // 发送消息并获取响应
                string response = await chatGLM.Chat(input, history);
                Console.WriteLine($"ChatGLM: {response}\n");

                // 更新对话历史
                if (string.IsNullOrEmpty(history))
                {
                    history = $"[[\"${input}\", \"{response}\"]]";
                }
                else
                {
                    // 移除最后的 ']' 并添加新的对话
                    history = history.TrimEnd(']');
                    history += $", [\"{input}\", \"{response}\"]]";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}\n");
            }
        }

        Console.WriteLine("感谢使用，再见！");
    }
}


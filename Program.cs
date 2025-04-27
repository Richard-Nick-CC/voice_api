using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // 配置ChatGLM
        string url = "https://open.bigmodel.cn/api/paas/v4/";  // 基础URL
        string apiKey = "c707cc7686924318b7fded7a1f7fa296.sSzbgplK46AOpR7C";
        // 智谱AI的API密钥格式说明
    

        // 创建ChatGLM实例
        var chatGLM = new ChatGLM(url, apiKey);

        Console.WriteLine("\n欢迎使用ChatGLM聊天程序！");
        Console.WriteLine("您可以尝试以下测试用例：");
        Console.WriteLine("1. 查询航班：'我想查询从北京到上海明天的航班'");
        Console.WriteLine("2. 查询票价：'MU2331明天的机票多少钱'");
        Console.WriteLine("输入 'exit' 退出程序\n");

        while (true)
        {
            Console.Write("您: ");
            string input = Console.ReadLine();

            if (input.ToLower() == "exit")
                break;

            try
            {
                Console.WriteLine("正在请求API，请稍候...");
                // 发送消息并获取响应
                string response = await chatGLM.Chat(input);
                Console.WriteLine($"ChatGLM: {response}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}\n");
            }
        }

        Console.WriteLine("感谢使用，再见！");
    }
}


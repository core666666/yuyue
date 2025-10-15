using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YuYue.Services;

/// <summary>
/// 提供本地文本文件的内容加载与轻量编码探测能力。
/// </summary>
public class TextContentService
{
    private static readonly Encoding[] FallbackEncodings;

    static TextContentService()
    {
        // 使 .NET 能够识别 GB18030、Big5 等东亚常用编码。
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        FallbackEncodings = new[]
        {
            Encoding.GetEncoding("GB18030"),  // 中文GBK/GB2312优先
            Encoding.UTF8,
            Encoding.Unicode,
            Encoding.GetEncoding("Big5")
        };
    }

    public async Task<string> LoadContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("指定的文件不存在。", filePath);
        }

        // 先尝试自动检测编码（包括BOM）
        try
        {
            await using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync(cancellationToken);
            
            // 检查是否有乱码（简单检测：是否有大量问号或特殊字符）
            if (!HasGarbledText(content))
            {
                return content;
            }
        }
        catch
        {
            // 继续尝试其他编码
        }

        // 如果自动检测失败，尝试各种编码
        foreach (var encoding in FallbackEncodings)
        {
            try
            {
                await using var stream = File.OpenRead(filePath);
                using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false);
                var content = await reader.ReadToEndAsync(cancellationToken);
                
                // 检查是否有乱码
                if (!HasGarbledText(content))
                {
                    return content;
                }
            }
            catch
            {
                // 尝试下一种编码
            }
        }

        // 所有备选方案失败时兜底为 GB18030
        return await File.ReadAllTextAsync(filePath, Encoding.GetEncoding("GB18030"), cancellationToken);
    }
    
    private static bool HasGarbledText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return true;
            
        // 检查前1000个字符
        var sample = text.Length > 1000 ? text.Substring(0, 1000) : text;
        
        int questionMarks = 0;
        int totalChars = 0;
        
        foreach (char c in sample)
        {
            totalChars++;
            if (c == '?' || c == '�')  // 问号或替换字符
                questionMarks++;
        }
        
        // 如果超过5%是问号/替换字符，认为是乱码
        return totalChars > 0 && (double)questionMarks / totalChars > 0.05;
    }
}

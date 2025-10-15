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
            Encoding.UTF8,
            Encoding.Unicode,
            Encoding.GetEncoding("GB18030"),
            Encoding.GetEncoding("Big5")
        };
    }

    public async Task<string> LoadContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("指定的文件不存在。", filePath);
        }

        foreach (var encoding in FallbackEncodings)
        {
            try
            {
                await using var stream = File.OpenRead(filePath);
                using var reader = new StreamReader(
                    stream,
                    encoding,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 4096,
                    leaveOpen: false);

                return await reader.ReadToEndAsync(cancellationToken);
            }
            catch (DecoderFallbackException)
            {
                // 尝试下一种编码。
            }
        }

        // 所有备选方案失败时兜底为 UTF8。
        return await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
    }
}

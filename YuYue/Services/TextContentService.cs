using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YuYue.Services;

/// <summary>
/// Loads plain text content from disk with lightweight encoding detection.
/// </summary>
public class TextContentService
{
    private static readonly Encoding[] FallbackEncodings =
    {
        Encoding.UTF8,
        Encoding.Unicode,
        Encoding.GetEncoding("GB18030"),
        Encoding.GetEncoding("Big5")
    };

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
                using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
                return await reader.ReadToEndAsync(cancellationToken);
            }
            catch (DecoderFallbackException)
            {
                // Try the next encoding.
            }
        }

        // Default to UTF8 if everything else fails.
        return await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
    }
}

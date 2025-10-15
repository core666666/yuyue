using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using YuYue.Models;

namespace YuYue.Services;

public class ChapterService
{
    private static readonly Regex[] ChapterPatterns = new[]
    {
        new Regex(@"^第[0-9零一二三四五六七八九十百千万]+[章节回集卷部篇][\s\:：]*.{0,30}$", RegexOptions.Multiline),
        new Regex(@"^Chapter\s+\d+[\s\:：]*.{0,30}$", RegexOptions.Multiline | RegexOptions.IgnoreCase),
        new Regex(@"^\d+[\.\、][\s]*.{1,30}$", RegexOptions.Multiline),
        new Regex(@"^[【\[]第[0-9零一二三四五六七八九十百千万]+[章节回集卷部篇][】\]][\s]*.{0,30}$", RegexOptions.Multiline),
    };

    public List<Chapter> ExtractChapters(string content)
    {
        var chapters = new List<Chapter>();
        var foundPositions = new SortedSet<int>();

        foreach (var pattern in ChapterPatterns)
        {
            var matches = pattern.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Success && match.Value.Trim().Length >= 2)
                {
                    foundPositions.Add(match.Index);
                }
            }
        }

        var positions = new List<int>(foundPositions);
        
        for (int i = 0; i < positions.Count; i++)
        {
            var startPos = positions[i];
            var endPos = i < positions.Count - 1 ? positions[i + 1] : content.Length;
            
            var titleEndPos = content.IndexOf('\n', startPos);
            if (titleEndPos == -1 || titleEndPos > startPos + 100)
                titleEndPos = Math.Min(startPos + 50, content.Length);
            
            var title = content.Substring(startPos, titleEndPos - startPos).Trim();
            
            chapters.Add(new Chapter
            {
                Title = title,
                StartOffset = startPos,
                Length = endPos - startPos
            });
        }

        if (chapters.Count == 0)
        {
            chapters.Add(new Chapter
            {
                Title = "全文",
                StartOffset = 0,
                Length = content.Length
            });
        }

        return chapters;
    }
}

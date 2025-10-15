using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YuYue.Models;

/// <summary>
/// Represents a locally stored book and its basic reading metadata.
/// </summary>
public partial class Book : ObservableObject
{
    [ObservableProperty]
    private Guid id = Guid.NewGuid();

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string? author;

    [ObservableProperty]
    private string filePath = string.Empty;

    [ObservableProperty]
    private DateTime lastOpenedUtc = DateTime.UtcNow;

    [ObservableProperty]
    private int currentOffset;

    [ObservableProperty]
    private int totalLength;
    
    [ObservableProperty]
    private List<Chapter> chapters = new();
    
    [ObservableProperty]
    private List<Bookmark> bookmarks = new();
    
    [ObservableProperty]
    private int totalReadingMinutes;
    
    [ObservableProperty]
    private DateTime createdUtc = DateTime.UtcNow;

    public double Progress => TotalLength == 0 ? 0 : Math.Clamp((double)CurrentOffset / TotalLength, 0, 1);

    partial void OnCurrentOffsetChanged(int value) => OnPropertyChanged(nameof(Progress));

    partial void OnTotalLengthChanged(int value) => OnPropertyChanged(nameof(Progress));
}

public class Chapter
{
    public string Title { get; set; } = string.Empty;
    public int StartOffset { get; set; }
    public int Length { get; set; }
}

public class Bookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Offset { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}

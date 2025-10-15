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

    public double Progress => TotalLength == 0 ? 0 : Math.Clamp((double)CurrentOffset / TotalLength, 0, 1);

    partial void OnCurrentOffsetChanged(int value) => OnPropertyChanged(nameof(Progress));

    partial void OnTotalLengthChanged(int value) => OnPropertyChanged(nameof(Progress));
}

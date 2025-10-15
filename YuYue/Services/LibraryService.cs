using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YuYue.Models;

namespace YuYue.Services;

/// <summary>
/// Manages persistence of the local bookshelf library.
/// </summary>
public class LibraryService
{
    private readonly string _storageDirectory;
    private readonly string _libraryFilePath;

    public LibraryService()
    {
        _storageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YuYue");
        _libraryFilePath = Path.Combine(_storageDirectory, "library.json");
    }

    public async Task<ObservableCollection<Book>> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_libraryFilePath))
            {
                Directory.CreateDirectory(_storageDirectory);
                return new ObservableCollection<Book>();
            }

            await using var stream = File.OpenRead(_libraryFilePath);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync(cancellationToken);

            var data = JsonConvert.DeserializeObject<List<BookDto>>(json);
            return data is null
                ? new ObservableCollection<Book>()
                : new ObservableCollection<Book>(data.Select(ToBook));
        }
        catch
        {
            // Corrupted json or IO issues shouldn't crash the app; start fresh.
            return new ObservableCollection<Book>();
        }
    }

    public async Task SaveAsync(IEnumerable<Book> books, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_storageDirectory);

        var data = books.Select(ToDto).ToList();
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);

        await File.WriteAllTextAsync(_libraryFilePath, json, cancellationToken);
    }

    private static Book ToBook(BookDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Author = dto.Author,
        FilePath = dto.FilePath,
        LastOpenedUtc = dto.LastOpenedUtc,
        CurrentOffset = dto.CurrentOffset,
        TotalLength = dto.TotalLength
    };

    private static BookDto ToDto(Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        FilePath = book.FilePath,
        LastOpenedUtc = book.LastOpenedUtc,
        CurrentOffset = book.CurrentOffset,
        TotalLength = book.TotalLength
    };

    private sealed class BookDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime LastOpenedUtc { get; set; }
        public int CurrentOffset { get; set; }
        public int TotalLength { get; set; }
    }
}

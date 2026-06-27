using BookStoreApi.Models;

namespace BookStoreApi.Data;

public class BookRepository
{
    private static int _nextId = 6;
    private static readonly List<Book> _books = new()
    {
        new Book { Id=1, Title="Clean Code",           Author="Robert C. Martin", Genre="Programación",  Price=45.90, Year=2008, InStock=true  },
        new Book { Id=2, Title="The Pragmatic Programmer", Author="David Thomas",  Genre="Programación",  Price=52.00, Year=1999, InStock=true  },
        new Book { Id=3, Title="Domain-Driven Design",  Author="Eric Evans",      Genre="Arquitectura",  Price=60.50, Year=2003, InStock=false },
        new Book { Id=4, Title="Designing Data-Intensive Apps", Author="Martin Kleppmann", Genre="Sistemas", Price=70.00, Year=2017, InStock=true  },
        new Book { Id=5, Title="Refactoring",           Author="Martin Fowler",   Genre="Programación",  Price=48.00, Year=2018, InStock=true  },
    };
    private static readonly object _lock = new();

    public IQueryable<Book> GetBooks() { lock(_lock) return _books.ToList().AsQueryable(); }

    public Book? GetById(int id) { lock(_lock) return _books.FirstOrDefault(b => b.Id == id); }

    public Book Add(BookInput input)
    {
        var book = new Book
        {
            Id      = Interlocked.Increment(ref _nextId),
            Title   = input.Title,
            Author  = input.Author,
            Genre   = input.Genre,
            Price   = input.Price,
            Year    = input.Year,
            InStock = true
        };
        lock (_lock) _books.Add(book);
        return book;
    }

    public Book? Delete(int id)
    {
        lock (_lock)
        {
            var book = _books.FirstOrDefault(b => b.Id == id);
            if (book != null) _books.Remove(book);
            return book;
        }
    }
}

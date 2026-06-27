using BookStoreApi.Data;
using BookStoreApi.Models;

namespace BookStoreApi.GraphQL.Queries;

public class Query
{
    [UseFiltering]
    [UseSorting]
    public IQueryable<Book> GetBooks([Service] BookRepository repo) => repo.GetBooks();

    public Book? GetBook(int id, [Service] BookRepository repo) => repo.GetById(id);
}

using BookStoreApi.Data;
using BookStoreApi.Models;
using HotChocolate.Subscriptions;

namespace BookStoreApi.GraphQL.Mutations;

public class Mutation
{
    public Book AddBook(BookInput input,
        [Service] BookRepository repo,
        [Service] ITopicEventSender sender)
    {
        var book = repo.Add(input);
        sender.SendAsync("BookAdded", book);
        return book;
    }

    public Book? DeleteBook(int id,
        [Service] BookRepository repo)
        => repo.Delete(id);
}

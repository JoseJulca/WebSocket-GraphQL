using BookStoreApi.Models;

namespace BookStoreApi.GraphQL.Subscriptions;

public class Subscription
{
    [Subscribe]
    [Topic("BookAdded")]
    public Book OnBookAdded([EventMessage] Book book) => book;
}

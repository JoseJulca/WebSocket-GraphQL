namespace BookStoreApi.Models;

public class Book
{
    public int    Id       { get; set; }
    public string Title    { get; set; } = "";
    public string Author   { get; set; } = "";
    public string Genre    { get; set; } = "";
    public double Price    { get; set; }
    public int    Year     { get; set; }
    public bool   InStock  { get; set; }
}

public class BookInput
{
    public string Title   { get; set; } = "";
    public string Author  { get; set; } = "";
    public string Genre   { get; set; } = "";
    public double Price   { get; set; }
    public int    Year    { get; set; }
}

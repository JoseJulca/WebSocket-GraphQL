using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookStoreWebApp.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _config;
    public string ApiUrl { get; private set; } = "";

    public IndexModel(IConfiguration config)
    {
        _config = config;
    }

    public void OnGet()
    {
        ApiUrl = _config["GraphQL:ApiUrl"] ?? "http://localhost:5100/graphql";
    }
}

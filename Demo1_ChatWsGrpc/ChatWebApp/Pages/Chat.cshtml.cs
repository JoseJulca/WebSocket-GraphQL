using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatWebApp.Pages;

public class ChatModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Sender { get; set; } = "Anónimo";

    [BindProperty(SupportsGet = true)]
    public string Room { get; set; } = "general";

    public IActionResult OnGet()
    {
        if (string.IsNullOrWhiteSpace(Sender))
            return RedirectToPage("/Index");
        return Page();
    }
}

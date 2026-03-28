using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace DemoWeb.Pages;
public class LoginModel : PageModel
{
    private const string DemoPassword = "Passw0rd!";
    [BindProperty]
    public LoginInput Input { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public void OnGet()
    {
    }
    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        if (!string.Equals(Input.Email, "demo@northwindfabrication.test", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Input.Password, DemoPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "The email or password is incorrect. Please try the demo account again.";
            return Page();
        }
        return RedirectToPage("/Status");
    }
    public sealed class LoginInput
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
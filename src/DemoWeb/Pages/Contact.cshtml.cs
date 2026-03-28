using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace DemoWeb.Pages;
public class ContactModel : PageModel
{
    [BindProperty]
    public ContactInput Input { get; set; } = new();
    [TempData]
    public string? SuccessMessage { get; set; }
    public IReadOnlyList<SelectListItem> TopicOptions { get; } =
    [
        new("Factory reporting", "Factory reporting"),
        new("Field service", "Field service"),
        new("Support", "Support"),
        new("Executive dashboard", "Executive dashboard")
    ];
    public void OnGet()
    {
    }
    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        SuccessMessage = $"Thanks {Input.Name}, our industrial apps team will reply within one business day.";
        return RedirectToPage();
    }
    public sealed class ContactInput
    {
        [Required]
        [Display(Name = "Full name")]
        public string Name { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Company { get; set; } = string.Empty;
        [Required]
        public string Topic { get; set; } = "Factory reporting";
        [Required]
        [MinLength(10)]
        public string Message { get; set; } = string.Empty;
    }
}

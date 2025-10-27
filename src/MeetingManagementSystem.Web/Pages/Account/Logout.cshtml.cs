using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Web.Pages.Account;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(SignInManager<User> signInManager, ILogger<LogoutModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    public bool LoggedOut { get; set; }

    public void OnGet()
    {
        LoggedOut = false;
    }

    public async Task<IActionResult> OnPost(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        
        LoggedOut = true;

        if (returnUrl != null)
        {
            return LocalRedirect(returnUrl);
        }
        else
        {
            return Page();
        }
    }
}

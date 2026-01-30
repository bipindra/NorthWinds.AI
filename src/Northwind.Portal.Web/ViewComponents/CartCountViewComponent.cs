using Microsoft.AspNetCore.Mvc;
using Northwind.Portal.Domain.Services;
using System.Security.Claims;
using System.Linq;

namespace Northwind.Portal.Web.ViewComponents;

public class CartCountViewComponent : ViewComponent
{
    private readonly ICartService _cartService;

    public CartCountViewComponent(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? UserClaimsPrincipal?.Identity?.Name;
        
        var count = 0;
        if (!string.IsNullOrEmpty(userId))
        {
            try
            {
                var cart = await _cartService.GetCartAsync(userId);
                count = cart?.Lines?.Sum(l => l.Quantity) ?? 0;
            }
            catch
            {
                // Ignore errors, show 0
            }
        }

        return View(count);
    }
}

using Northwind.Portal.Domain.DTOs;

namespace Northwind.Portal.Domain.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);
    Task<bool> AddToCartAsync(string userId, int productId, short quantity);
    Task<bool> UpdateQuantityAsync(string userId, int cartLineId, short quantity);
    Task<bool> RemoveFromCartAsync(string userId, int cartLineId);
    Task<bool> ClearCartAsync(string userId);
}

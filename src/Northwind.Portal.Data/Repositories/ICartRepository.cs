using Northwind.Portal.Domain.Entities;

namespace Northwind.Portal.Data.Repositories;

public interface ICartRepository
{
    Task<CartHeader?> GetCartByUserIdAsync(string userId);
    Task<CartHeader> CreateCartAsync(CartHeader cart);
    Task<bool> UpdateCartAsync(CartHeader cart);
    Task<bool> DeleteCartAsync(int cartHeaderId);
    Task<CartLine?> GetCartLineByIdAsync(int cartLineId);
    Task<bool> DeleteCartLineAsync(int cartLineId);
}

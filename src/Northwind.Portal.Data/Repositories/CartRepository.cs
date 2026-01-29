using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Data.Contexts;
using Northwind.Portal.Domain.Entities;

namespace Northwind.Portal.Data.Repositories;

public class CartRepository : ICartRepository
{
    private readonly NorthwindDbContext _context;

    public CartRepository(NorthwindDbContext context)
    {
        _context = context;
    }

    public async Task<CartHeader?> GetCartByUserIdAsync(string userId)
    {
        return await _context.CartHeaders
            .Include(ch => ch.CartLines)
                .ThenInclude(cl => cl.Product)
            .FirstOrDefaultAsync(ch => ch.UserId == userId);
    }

    public async Task<CartHeader> CreateCartAsync(CartHeader cart)
    {
        _context.CartHeaders.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    public async Task<bool> UpdateCartAsync(CartHeader cart)
    {
        cart.UpdatedAt = DateTime.UtcNow;
        _context.CartHeaders.Update(cart);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> DeleteCartAsync(int cartHeaderId)
    {
        var cart = await _context.CartHeaders
            .Include(ch => ch.CartLines)
            .FirstOrDefaultAsync(ch => ch.Id == cartHeaderId);

        if (cart == null) return false;

        _context.CartLines.RemoveRange(cart.CartLines);
        _context.CartHeaders.Remove(cart);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<CartLine?> GetCartLineByIdAsync(int cartLineId)
    {
        return await _context.CartLines
            .Include(cl => cl.Product)
            .FirstOrDefaultAsync(cl => cl.Id == cartLineId);
    }

    public async Task<bool> DeleteCartLineAsync(int cartLineId)
    {
        var cartLine = await _context.CartLines.FindAsync(cartLineId);
        if (cartLine == null) return false;

        _context.CartLines.Remove(cartLine);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }
}

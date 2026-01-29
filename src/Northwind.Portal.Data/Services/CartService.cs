using Northwind.Portal.Domain.DTOs;
using Northwind.Portal.Domain.Entities;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Data.Repositories;
using Northwind.Portal.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Northwind.Portal.Data.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly NorthwindDbContext _context;

    public CartService(ICartRepository cartRepository, IProductRepository productRepository, NorthwindDbContext context)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _context = context;
    }

    public async Task<CartDto> GetCartAsync(string userId)
    {
        var cart = await _cartRepository.GetCartByUserIdAsync(userId);
        
        if (cart == null)
        {
            return new CartDto { CartHeaderId = 0 };
        }

        return new CartDto
        {
            CartHeaderId = cart.Id,
            Lines = cart.CartLines.Select(cl => new CartLineDto
            {
                Id = cl.Id,
                ProductId = cl.ProductId,
                ProductName = cl.Product.ProductName,
                Quantity = cl.Quantity,
                UnitPrice = cl.UnitPrice,
                IsDiscontinued = cl.Product.Discontinued,
                UnitsInStock = cl.Product.UnitsInStock
            }).ToList()
        };
    }

    public async Task<bool> AddToCartAsync(string userId, int productId, short quantity)
    {
        if (quantity <= 0)
            return false;

        var product = await _productRepository.GetProductByIdAsync(productId);
        if (product == null || product.Discontinued)
            return false;

        var cart = await _cartRepository.GetCartByUserIdAsync(userId);
        
        if (cart == null)
        {
            cart = new CartHeader
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CartLines = new List<CartLine>()
            };
            cart = await _cartRepository.CreateCartAsync(cart);
        }

        var existingLine = cart.CartLines.FirstOrDefault(cl => cl.ProductId == productId);
        if (existingLine != null)
        {
            existingLine.Quantity += quantity;
        }
        else
        {
            cart.CartLines.Add(new CartLine
            {
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.UnitPrice ?? 0
            });
        }

        return await _cartRepository.UpdateCartAsync(cart);
    }

    public async Task<bool> UpdateQuantityAsync(string userId, int cartLineId, short quantity)
    {
        if (quantity <= 0)
            return false;

        var cartLine = await _cartRepository.GetCartLineByIdAsync(cartLineId);
        if (cartLine == null)
            return false;

        var cart = await _cartRepository.GetCartByUserIdAsync(userId);
        if (cart == null || cart.Id != cartLine.CartHeaderId)
            return false;

        cartLine.Quantity = quantity;
        cart.UpdatedAt = DateTime.UtcNow;
        
        _context.CartLines.Update(cartLine);
        _context.CartHeaders.Update(cart);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> RemoveFromCartAsync(string userId, int cartLineId)
    {
        var cartLine = await _cartRepository.GetCartLineByIdAsync(cartLineId);
        if (cartLine == null)
            return false;

        var cart = await _cartRepository.GetCartByUserIdAsync(userId);
        if (cart == null || cart.Id != cartLine.CartHeaderId)
            return false;

        return await _cartRepository.DeleteCartLineAsync(cartLineId);
    }

    public async Task<bool> ClearCartAsync(string userId)
    {
        var cart = await _cartRepository.GetCartByUserIdAsync(userId);
        if (cart == null)
            return true;

        return await _cartRepository.DeleteCartAsync(cart.Id);
    }
}

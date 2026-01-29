namespace Northwind.Portal.Domain.DTOs;

public class CartDto
{
    public int CartHeaderId { get; set; }
    public List<CartLineDto> Lines { get; set; } = new();
    public decimal SubTotal => Lines.Sum(l => l.LineTotal);
    public int TotalItems => Lines.Sum(l => l.Quantity);
}

public class CartLineDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public short Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
    public bool IsDiscontinued { get; set; }
    public short? UnitsInStock { get; set; }
}

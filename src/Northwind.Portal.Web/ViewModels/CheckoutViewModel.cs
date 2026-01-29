using System.ComponentModel.DataAnnotations;

namespace Northwind.Portal.Web.ViewModels;

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Ship To Name is required")]
    [StringLength(40, ErrorMessage = "Name cannot exceed 40 characters")]
    [Display(Name = "Ship To Name")]
    public string ShipName { get; set; } = null!;

    [Required(ErrorMessage = "Address is required")]
    [StringLength(60, ErrorMessage = "Address cannot exceed 60 characters")]
    [Display(Name = "Address")]
    public string ShipAddress { get; set; } = null!;

    [Required(ErrorMessage = "City is required")]
    [StringLength(15, ErrorMessage = "City cannot exceed 15 characters")]
    [Display(Name = "City")]
    public string ShipCity { get; set; } = null!;

    [StringLength(15, ErrorMessage = "Region cannot exceed 15 characters")]
    [Display(Name = "Region")]
    public string? ShipRegion { get; set; }

    [StringLength(10, ErrorMessage = "Postal Code cannot exceed 10 characters")]
    [Display(Name = "Postal Code")]
    public string? ShipPostalCode { get; set; }

    [Required(ErrorMessage = "Country is required")]
    [StringLength(15, ErrorMessage = "Country cannot exceed 15 characters")]
    [Display(Name = "Country")]
    public string ShipCountry { get; set; } = null!;

    [StringLength(50, ErrorMessage = "PO Number cannot exceed 50 characters")]
    [Display(Name = "PO Number (Optional)")]
    public string? PoNumber { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    [Display(Name = "Notes (Optional)")]
    public string? Notes { get; set; }
}

namespace QuanLyBanDienThoai.Models;

public class PhoneDetailMetadata
{
    public string ProductId { get; set; } = string.Empty;
    public string ModelSeries { get; set; } = string.Empty;
    public string ConditionType { get; set; } = string.Empty;
    public string ConditionGrade { get; set; } = string.Empty;
    public string ConditionDescription { get; set; } = string.Empty;
    public int WarrantyMonths { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public string ColorCode { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ScreenSize { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public string RAM { get; set; } = string.Empty;
    public string Storage { get; set; } = string.Empty;
    public string Battery { get; set; } = string.Empty;
    public string Camera { get; set; } = string.Empty;
    public string ChargingTechnology { get; set; } = string.Empty;
    public string Ports { get; set; } = string.Empty;
    public string Weight { get; set; } = string.Empty;
}

public class ProductReviewModel
{
    public string ProductId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CustomerOrderRequestModel
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.RegularExpression(@"^0[35789]\d{8}$")]
    public string Phone { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(250)]
    public string Address { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required]
    public string CheckoutToken { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string? Note { get; set; }
    public List<CustomerOrderRequestItem> Items { get; set; } = new();
}

public class CustomerOrderRequestItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

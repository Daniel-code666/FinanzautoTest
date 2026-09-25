using System.ComponentModel.DataAnnotations;

namespace Finanzauto.Application.Orders;

public sealed class OrderDetailRequest
{
    [Range(1, int.MaxValue)]
    public int? OrderDetailId { get; set; }

    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0", "1")]
    public decimal Discount { get; set; }
}

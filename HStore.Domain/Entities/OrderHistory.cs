using HStore.Domain.Enums;

namespace HStore.Domain.Entities;

public class OrderHistory : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; }

    public OrderStatus PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }

    public string? Notes { get; set; }
}
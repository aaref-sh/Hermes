﻿using HStore.Domain.Enums;

namespace HStore.Application.DTOs;

public class OrderPreviewDto
{
    public DateTime OrderDate { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public AddressDto ShippingAddress { get; set; }
    public AddressDto BillingAddress { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } 
    public decimal TotalAmount { get; set; }
    public int UserId { get; set; }
    public PaymentMethodType PaymentMethod { get; set; }
    public decimal? CodFee { get; set; }
    public IEnumerable<ShippingRate> AvailableShippingRates { get; set; } 
}
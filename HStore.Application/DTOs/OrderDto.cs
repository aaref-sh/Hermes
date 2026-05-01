﻿using HStore.Domain.Enums;

namespace HStore.Application.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public AddressDto ShippingAddress { get; set; }
    public AddressDto BillingAddress { get; set; }
    public string Currency { get; set; }
    public decimal TotalAmount { get; set; }
    public int UserId { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = [];

    public PaymentMethodType PaymentMethod { get; set; }
    public string PaymentIntentId { get; set; }
    public string CheckoutSessionId { get; set; }
    
    // Pay-on-Delivery (COD) tracking properties
    public bool IsCodCollected { get; set; }
    public DateTime? CodCollectionDate { get; set; }
    public decimal? CodFee { get; set; }
}

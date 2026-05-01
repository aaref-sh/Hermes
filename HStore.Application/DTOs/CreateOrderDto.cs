﻿using HStore.Domain.Enums;

namespace HStore.Application.DTOs;

public class CreateOrderDto
{
    public int UserId { get; set; }
    public AddressDto ShippingAddress { get; set; }
    public AddressDto BillingAddress { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentMethodType PaymentMethod { get; set; }
    public decimal? CodFee { get; set; }
    public string ShippingMethod { get; set; }
    public string CouponCode { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = [];
}
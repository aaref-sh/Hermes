﻿﻿﻿﻿﻿﻿﻿using AutoMapper;
using HStore.Application.DTOs;
using HStore.Application.Exceptions;
using HStore.Application.Interfaces;
using HStore.Domain.Entities;
using HStore.Domain.Enums;
using HStore.Domain.Interfaces;
using HStore.Domain.Settings;
using Microsoft.Extensions.Options;

namespace HStore.Application.Services;

public class OrderService(
    IUnitOfWork unitOfWork,
    IInventoryService inventoryService,
    ICartService cartService,
    IShippingService shippingService,
    IPaymentService paymentService,
    IEmailService emailService,
    IOptions<WarehouseAddressSettings> warehouseAddressSettings,
    IMapper mapper) : IOrderService
{
    /// <summary>
    /// Gets a preview of an order based on the provided order details.
    /// </summary>
    /// <param name="orderDto">The CreateOrderDto object containing the order details to preview.</param>
    /// <returns>An OrderPreviewDto object representing the preview of the order, or null if the order cannot be created.</returns>
    public async Task<OrderPreviewDto?> GetOrderPreviewAsync(CreateOrderDto orderDto)
    {
        var productIds = orderDto.OrderItems.Select(oi => oi.ProductId).ToList();
        var products = (await unitOfWork.Products.GetByIdsAsync(productIds)).ToDictionary(p => p.Id, p => p);

        var shippingRateRequests = new List<ShippingRateRequest>();
        foreach (var orderItemDto in orderDto.OrderItems)
        {
            if (!products.TryGetValue(orderItemDto.ProductId, out var product))
                throw new NotFoundException($"Product with ID {orderItemDto.ProductId} not found.");

            if (!await inventoryService.IsInStockAsync(orderItemDto.ProductId, orderItemDto.Quantity))
                throw new BadRequestException(
                    $"Product '{product.Name}' is out of stock or insufficient quantity available.");

            shippingRateRequests.Add(new ShippingRateRequest
            {
                OriginPostalCode = product.HostedAt == HostedAt.Store
                    ? product.Seller.Address.PostalCode
                    : GetWarehouseAddress().PostalCode,
                DestinationPostalCode = orderDto.ShippingAddress.PostalCode,
                Weight = product.Weight,
                Width = product.Width,
                Height = product.Height,
                Length = product.Length
            });
        }

        var availableShippingRates = await shippingService.GetShippingRatesAsync(shippingRateRequests);
        var totalAmount = CalculateTotalAmount(orderDto.OrderItems);

        return new OrderPreviewDto
        {
            OrderDate = DateTime.UtcNow,
            OrderStatus = OrderStatus.Pending,
            ShippingAddress = orderDto.ShippingAddress,
            BillingAddress = orderDto.BillingAddress,
            OrderItems = orderDto.OrderItems,
            TotalAmount = totalAmount + (orderDto.PaymentMethod == PaymentMethodType.PayOnDelivery ? orderDto.CodFee ?? 0 : 0),
            UserId = orderDto.UserId,
            PaymentMethod = orderDto.PaymentMethod,
            CodFee = orderDto.CodFee,
            AvailableShippingRates = availableShippingRates
        };
    }

    /// <summary>
    /// Creates a new order based on the provided order details.
    /// </summary>
    /// <param name="orderDto">The CreateOrderDto object containing the order details to create.</param>
    /// <returns>An OrderDto object representing the newly created order.</returns>
    public async Task<OrderDto?> CreateOrderAsync(CreateOrderDto orderDto)
    {
        var productIds = orderDto.OrderItems.Select(oi => oi.ProductId).ToList();
        var products = (await unitOfWork.Products.GetByIdsAsync(productIds)).ToDictionary(p => p.Id, p => p);

        await unitOfWork.BeginTransactionAsync();
        try
        {
            var shippingRateRequests = new List<ShippingRateRequest>();
            foreach (var orderItemDto in orderDto.OrderItems)
            {
                if (!products.TryGetValue(orderItemDto.ProductId, out var product))
                    throw new NotFoundException($"Product with ID {orderItemDto.ProductId} not found.");

                if (!await inventoryService.IsInStockAsync(orderItemDto.ProductId, orderItemDto.Quantity))
                    throw new BadRequestException(
                        $"Product '{product.Name}' is out of stock or insufficient quantity available.");

                await inventoryService.ReserveStockAsync(product.Id, orderItemDto.Quantity);

                shippingRateRequests.Add(new ShippingRateRequest
                {
                    OriginPostalCode = product.HostedAt == HostedAt.Store
                        ? product.Seller.Address.PostalCode
                        : GetWarehouseAddress().PostalCode,
                    DestinationPostalCode = orderDto.ShippingAddress.PostalCode,
                    Weight = product.Weight,
                    Width = product.Width,
                    Height = product.Height,
                    Length = product.Length
                });
            }

            var selectedShippingRate = (await shippingService.GetShippingRatesAsync(shippingRateRequests))
                .FirstOrDefault(rate => rate.Carrier == orderDto.ShippingMethod);

            if (selectedShippingRate == null)
                throw new BadRequestException("Unable to calculate shipping rates.");

            var cart = await cartService.GetCartByUserIdAsync(orderDto.UserId);
            if (cart == null)
            {
                throw new NotFoundException($"Cart not found for user with ID {orderDto.UserId}.");
            }

            var order = new Order
            {
                OrderDate = DateTime.UtcNow,
                OrderStatus = OrderStatus.Pending,
                PaymentMethod = orderDto.PaymentMethod,
                CodFee = orderDto.CodFee,

                ShippingAddress = mapper.Map<Address>(orderDto.ShippingAddress),
                BillingAddress = mapper.Map<Address>(orderDto.BillingAddress),
                TotalAmount = cart.TotalPrice + selectedShippingRate.TotalRate + (orderDto.PaymentMethod == PaymentMethodType.PayOnDelivery ? orderDto.CodFee ?? 0 : 0),
                UserId = orderDto.UserId,
                Currency = orderDto.Currency
            };

            foreach (var orderItemDto in orderDto.OrderItems)
            {
                if (!products.TryGetValue(orderItemDto.ProductId, out var product)) continue;

                order.OrderItems.Add(new OrderItem
                {
                    ProductId = orderItemDto.ProductId,
                    ProductVariantId = orderItemDto.ProductVariantId,
                    Quantity = orderItemDto.Quantity,
                    PriceAtPurchase = orderItemDto.PriceAtPurchase
                });
                await inventoryService.UpdateQuantityAsync(product.Id, orderItemDto.Quantity, Operator.Subtract);
            }

            await unitOfWork.Orders.AddAsync(order);
            var orderHistory = new OrderHistory
            {
                OrderId = order.Id,
                PreviousStatus = OrderStatus.Pending,
                NewStatus = OrderStatus.Pending,
                Notes = orderDto.PaymentMethod == PaymentMethodType.PayOnDelivery 
                    ? "Order created with Pay on Delivery payment method." 
                    : "Order created."
            };


            await unitOfWork.OrderHistory.AddAsync(orderHistory);

            await unitOfWork.SaveChangesAsync();

            await unitOfWork.Carts.ClearCartAsync(cart.Id);

            await unitOfWork.CommitTransactionAsync();

            var returnedOrder = mapper.Map<OrderDto>(order);
            await emailService.SendOrderConfirmationEmailAsync(returnedOrder);
            return returnedOrder;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync();
            foreach (var orderItemDto in orderDto.OrderItems)
            {
                if (products.TryGetValue(orderItemDto.ProductId, out var product))
                {
                    await inventoryService.ReleaseStockAsync(product.Id, orderItemDto.Quantity);
                }
            }

            throw new BadRequestException(
                $"An error occurred while processing your order. {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves a collection of orders associated with a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user whose orders to retrieve.</param>
    /// <returns>An IEnumerable of OrderDto objects representing the user's orders.</returns>
    public async Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(int userId)
    {
        var orders = await unitOfWork.Orders.GetOrdersByUserAsync(userId);
        return mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    /// <summary>
    /// Retrieves detailed information for a specific order.
    /// </summary>
    /// <param name="orderId">The ID of the order to retrieve details for.</param>
    /// <returns>The retrieved OrderDto object, or null if no matching order is found.</returns>
    public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
    {
        var order = await unitOfWork.Orders.GetOrderDetailsAsync(orderId);
        if (order == null)
            throw new NotFoundException($"Order with ID {orderId} not found.");

        return mapper.Map<OrderDto>(order);
    }

    /// <summary>
    /// Retrieves a paged collection of all orders.
    /// </summary>
    /// <param name="page">The page number to retrieve. Defaults to 1.</param>
    /// <param name="pageSize">The number of items per page. Defaults to 10.</param>
    /// <returns>A PagedResult object containing the retrieved orders and pagination information.</returns>
    public async Task<PagedResult<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 10)
    {
        var query = unitOfWork.Orders.GetAllAsync();
        query = query.Skip((page - 1) * pageSize).Take(pageSize);
        var orders = await unitOfWork.Orders.ExecuteQueryAsync(query);
        var totalCount = query.Count();

        return new PagedResult<OrderDto>
        {
            Items = mapper.Map<IEnumerable<OrderDto>>(orders),
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    /// <param name="orderId">The ID of the order to update.</param>
    /// <param name="orderDto">The OrderDto object containing the updated order details.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task UpdateOrderAsync(int orderId, OrderDto orderDto)
    {
        if (orderDto.OrderItems.Count == 0)
        {
            throw new BadRequestException("Order must contain at least one item.");
        }
        
        var order = await unitOfWork.Orders.GetOrderDetailsAsync(orderId);
        if (order == null)
            throw new NotFoundException($"Order with ID {orderId} not found.");
        
        orderDto.UserId = order.UserId;
        order = mapper.Map(orderDto, order);
        await unitOfWork.Orders.UpdateAsync(order);
    }

    /// <summary>
    /// Updates the status of an existing order.
    /// </summary>
    /// <param name="orderId">The ID of the order to update.</param>
    /// <param name="newStatus">The new status to assign to the order.</param>
    /// <param name="notes">Optional notes to be added to the order history.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? notes = null)
    {
        await unitOfWork.BeginTransactionAsync();
        try
        {
            var order = await unitOfWork.Orders.GetOrderDetailsAsync(orderId);
            if (order == null)
                throw new NotFoundException($"Order with ID {orderId} not found.");

            if (!IsValidStatusTransition(order.OrderStatus, newStatus, order.PaymentMethod))
                throw new BadRequestException(
                    $"Invalid order status transition from {order.OrderStatus} to {newStatus}.");


            var orderHistory = new OrderHistory
            {
                OrderId = order.Id,
                PreviousStatus = order.OrderStatus,
                NewStatus = newStatus,
                Notes = notes
            };
            await unitOfWork.OrderHistory.AddAsync(orderHistory);

            order.OrderStatus = newStatus;
            await unitOfWork.Orders.UpdateAsync(order);

            await unitOfWork.CommitTransactionAsync();
            
            if (newStatus is OrderStatus.Shipped or OrderStatus.Delivered)
            {
                await emailService.SendShippingUpdateEmailAsync(mapper.Map<OrderDto>(order), newStatus.ToString()); 
            }
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw new BadRequestException(
                $"An error occurred while updating order status for order with ID {orderId}. {ex.Message}");
        }
    }
    
    /// <summary>
    /// Cancels an existing order.
    /// </summary>
    /// <param name="orderId">The ID of the order to cancel.</param>
    /// <returns>True if the order was successfully canceled, false otherwise.</returns>
    public async Task<bool> CancelOrderAsync(int orderId)
    {
        await unitOfWork.BeginTransactionAsync();
        try
        {
            var order = await unitOfWork.Orders.GetOrderDetailsAsync(orderId);
            if (order == null)
            {
                throw new NotFoundException($"Order with ID {orderId} not found.");
            }

            if (order.OrderStatus is OrderStatus.Delivered or OrderStatus.Cancelled)
            {
                throw new BadRequestException(
                    $"Order cannot be cancelled in its current status ({order.OrderStatus}).");
            }

            var orderHistory = new OrderHistory
            {
                OrderId = order.Id,
                PreviousStatus = order.OrderStatus,
                NewStatus = OrderStatus.Cancelled,
                Notes = "Order cancelled."
            };
            await unitOfWork.OrderHistory.AddAsync(orderHistory);

            // Capture the payment method and status before cancellation for refund check
            var paymentMethod = order.PaymentMethod;
            var statusBeforeCancellation = order.OrderStatus;

            order.OrderStatus = OrderStatus.Cancelled;
            await unitOfWork.Orders.UpdateAsync(order);

            foreach (var orderItem in order.OrderItems)
            {
                await inventoryService.ReleaseStockAsync(orderItem.ProductId, orderItem.Quantity);
            }

            // Only refund if paid via Card and status was Pending or Paid (not for PayOnDelivery)
            if (paymentMethod == PaymentMethodType.Card && statusBeforeCancellation is OrderStatus.Pending or OrderStatus.Paid)
            {
                var refundDto = new CreateRefundDto
                {
                    OrderId = order.Id,
                    PaymentIntentId = order.PaymentIntentId,
                    Amount = order.TotalAmount,
                };

                var refund = await paymentService.CreateRefundAsync(refundDto);
                if (refund == null)
                    throw new PaymentException($"Failed to create refund for order with ID {orderId} with amount {order.TotalAmount} due to payment error.");
            }

            await unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw new BadRequestException($"An error occurred while cancelling order with ID {orderId}.");
        }
    }

    /// <summary>
    /// Deletes an existing order.
    /// </summary>
    /// <param name="orderId">The ID of the order to delete.</param>
    /// <returns>True if the order was successfully deleted, false otherwise.</returns>
    public async Task<bool> DeleteOrderAsync(int orderId)
    {
        var order = await unitOfWork.Orders.GetOrderDetailsAsync(orderId);
        if (order == null)
            throw new NotFoundException($"Order with ID {orderId} not found.");

        await unitOfWork.Orders.DeleteAsync(order);
        return true;
    }
    
    /// <summary>
    /// Calculates the total amount of an order
    /// </summary>
    /// <param name="orderItems">List of order items</param>
    /// <returns></returns>
    private decimal CalculateTotalAmount(List<OrderItemDto> orderItems)
    {
        decimal total = 0;

        foreach (var item in orderItems)
        {
            total += item.Quantity * item.PriceAtPurchase;
        }

        return total;
    }

    /// <summary>
    /// Checks if an order status transition is valid
    /// </summary>
    /// <param name="currentStatus">Current order status</param>
    /// <param name="newStatus">New order status</param>
    /// <param name="paymentMethod">The payment method used for the order</param>
    /// <returns></returns>
    private bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus, PaymentMethodType paymentMethod)
    {
        return currentStatus switch
        {
            // Pending: can go to Paid (online payment confirmed), Processing (COD starts fulfillment), or Cancelled
            OrderStatus.Pending => paymentMethod == PaymentMethodType.PayOnDelivery
                ? newStatus is OrderStatus.Processing or OrderStatus.Cancelled
                : newStatus is OrderStatus.Paid or OrderStatus.Cancelled,
            // Paid: can go to Processing (fulfillment) or Cancelled
            OrderStatus.Paid => newStatus is OrderStatus.Processing or OrderStatus.Cancelled,
            // Processing: can go to Shipped or Cancelled
            OrderStatus.Processing => newStatus is OrderStatus.Shipped or OrderStatus.Cancelled,
            // Shipped: can go to Delivered, or to Paid (COD payment collection), or Cancelled
            OrderStatus.Shipped => paymentMethod == PaymentMethodType.PayOnDelivery
                ? newStatus is OrderStatus.Delivered or OrderStatus.Paid or OrderStatus.Cancelled
                : newStatus == OrderStatus.Delivered,
            // Delivered: can go to Paid (COD payment collection if not already paid)
            OrderStatus.Delivered => paymentMethod == PaymentMethodType.PayOnDelivery && newStatus == OrderStatus.Paid,
            _ => false
        };
    }

    
/// <summary>
    /// Collects payment for a Pay-on-Delivery (COD) order.
    /// </summary>
    /// <param name="orderId">The ID of the order to collect payment for.</param>
    /// <param name="codFee">Optional COD fee amount.</param>
    /// <returns>True if the payment was successfully collected, false otherwise.</returns>
    public async Task<bool> CollectCodPaymentAsync(int orderId, decimal? codFee = null)
    {
        await unitOfWork.BeginTransactionAsync();
        try
        {
            var order = await unitOfWork.Orders.GetOrderDetailsAsync(orderId);
            if (order == null)
                throw new NotFoundException($"Order with ID {orderId} not found.");

            if (order.PaymentMethod != PaymentMethodType.PayOnDelivery)
                throw new BadRequestException("This order is not paid on delivery.");

            // Check if payment has already been collected
            if (order.IsCodCollected)
                throw new BadRequestException("COD payment has already been collected for this order.");

            // Capture previous status before updating
            var previousStatus = order.OrderStatus;

            // Update order with COD collection details
            order.IsCodCollected = true;
            order.CodCollectionDate = DateTime.UtcNow;
            order.CodFee = codFee;
            order.OrderStatus = OrderStatus.Paid;

            var orderHistory = new OrderHistory
            {
                OrderId = order.Id,
                PreviousStatus = previousStatus,
                NewStatus = OrderStatus.Paid,
                Notes = $"COD payment collected. Fee: {codFee}"
            };
            await unitOfWork.OrderHistory.AddAsync(orderHistory);

            await unitOfWork.Orders.UpdateAsync(order);
            await unitOfWork.CommitTransactionAsync();

            var updatedOrder = mapper.Map<OrderDto>(order);
            await emailService.SendOrderConfirmationEmailAsync(updatedOrder);

            return true;
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw new BadRequestException($"An error occurred while collecting COD payment for order with ID {orderId}.");
        }
    }

    /// <summary>
    /// Gets the warehouse address
    /// </summary>
    /// <returns></returns>
    private Address GetWarehouseAddress()
    {
        return new Address
        {
            Street = warehouseAddressSettings.Value.Street,
            City = warehouseAddressSettings.Value.City,
            State = warehouseAddressSettings.Value.State,
            Country = warehouseAddressSettings.Value.Country,
            PostalCode = warehouseAddressSettings.Value.PostalCode
        };
    }
}

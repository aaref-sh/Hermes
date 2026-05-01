using AutoMapper;
using HStore.Application.DTOs;
using HStore.Domain.Entities;

namespace HStore.API.Mappers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Address Mapping
        CreateMap<AddressDto, Address>().ReverseMap();

        // Category Mappings
        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.ParentCategoryName, 
                       opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name: null));
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>();

        // Cart Mappings
        CreateMap<Cart, CartDto>();
        CreateMap<CartItem, CartItemDto>();

        // Coupon Mappings
        CreateMap<Coupon, CouponDto>().ReverseMap();
        CreateMap<CreateCouponDto, Coupon>();
        CreateMap<UpdateCouponDto, Coupon>();

        // Order Mappings
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.ShippingAddress, opt => opt.MapFrom(src => src.ShippingAddress))
            .ForMember(dest => dest.BillingAddress, opt => opt.MapFrom(src => src.BillingAddress))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod))
            .ForMember(dest => dest.IsCodCollected, opt => opt.MapFrom(src => src.IsCodCollected))
            .ForMember(dest => dest.CodCollectionDate, opt => opt.MapFrom(src => src.CodCollectionDate))
            .ForMember(dest => dest.CodFee, opt => opt.MapFrom(src => src.CodFee));

        CreateMap<OrderItem, OrderItemDto>().ReverseMap();
        CreateMap<CreateOrderDto, Order>()
            .ForMember(dest => dest.ShippingAddress, opt => opt.MapFrom(src => src.ShippingAddress))
            .ForMember(dest => dest.BillingAddress, opt => opt.MapFrom(src => src.BillingAddress))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod));

        // Product Mappings
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryIds, opt => opt.MapFrom(src => src.Categories.Select(c => c.Id).ToList()))
            .ForMember(dest => dest.CategoryNames, opt => opt.MapFrom(src => src.Categories.Select(c => c.Name).ToList()))
            .ForMember(dest => dest.Reviews, opt => opt.MapFrom(src => src.Reviews));
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();

        // Product Variant Mappings
        CreateMap<ProductVariant, ProductVariantDto>().ReverseMap();
        CreateMap<CreateProductVariantDto, ProductVariant>();
        CreateMap<UpdateProductVariantDto, ProductVariant>();

        // Product Media Mappings
        CreateMap<ProductMedia, ProductMediaDto>()
            .ForMember(dest => dest.MediaType, opt => opt.MapFrom(src => src.MediaType.ToString()));

        // Product Variant Option Mappings
        CreateMap<ProductVariantOption, ProductVariantOptionDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.VariantOptionType));

        CreateMap<ProductVariantOptionDto, ProductVariantOption>()
            .ForMember(dest => dest.VariantOptionType, opt => opt.MapFrom(src => src.Type));

        // Review Mappings
        CreateMap<Review, ReviewDto>().ReverseMap();
        CreateMap<CreateReviewDto, Review>();
        
        // User Mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address));
        CreateMap<UserDto, User>()
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address));
    }
}
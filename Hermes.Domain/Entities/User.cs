using Microsoft.AspNetCore.Identity;

namespace Hermes.Domain.Entities;

public class User : IdentityUser<int>, IBaseEntity
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string Role { get; set; }

    // In case of a Seller role
    public int Rating { get; set; }

    // Navigation Properties
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public Cart Cart { get; set; }
    public int CartId { get; set; }

    public int AddressId { get; set; }
    public Address Address { get; set; }
    public Guid Guid { get; set; } = Guid.NewGuid();
}
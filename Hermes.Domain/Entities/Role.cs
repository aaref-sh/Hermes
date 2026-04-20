using Microsoft.AspNetCore.Identity;

namespace Hermes.Domain.Entities;

public class Role : IdentityRole<int>, IBaseEntity
{
    public Guid Guid {get; set;}
}
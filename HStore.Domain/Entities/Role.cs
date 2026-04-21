using Microsoft.AspNetCore.Identity;

namespace HStore.Domain.Entities;

public class Role : IdentityRole<int>, IBaseEntity
{
    public Guid Guid {get; set;}
}
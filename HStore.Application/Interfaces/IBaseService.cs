using HStore.Application.DTOs;
using HStore.Domain.Entities;
using System.Threading.Tasks;

namespace HStore.Application.Interfaces;

public interface IBaseService<T> where T : class, IBaseEntity
{
    Task<PagedResult<TDto>> GetWithFilterAsync<TDto>(BaseFilter<T> filter, string[] includes = null);
}

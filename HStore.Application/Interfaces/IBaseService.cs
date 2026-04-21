using HStore.Application.DTOs;
using System.Threading.Tasks;

namespace HStore.Application.Interfaces;

public interface IBaseService<T>
{
    Task<PagedResult<T>> GetWithFilterAsync(FilterParams filter);
}

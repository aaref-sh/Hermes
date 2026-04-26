using AutoMapper;
using HStore.Application.DTOs;
using HStore.Application.Interfaces;
using HStore.Domain.Entities;
using HStore.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
 
using System.Linq.Expressions;

namespace HStore.Application.Services;

public abstract class BaseService<T, TDto>(IGenericRepository<T> repository, IMapper mapper) : IBaseService<T> 
    where T : class, IBaseEntity 
    where TDto : class
{
    protected readonly IGenericRepository<T> _repository = repository;
    protected readonly IMapper _mapper = mapper;

    public async Task<PagedResult<TDto>> GetWithFilterAsync<TDto>(BaseFilter<T> filter, string[] includes = null)
    {
        var query = _repository.GetAllAsync();
        foreach (var include in includes ?? []) query = query.Include(include);
        var result = await filter.ApplyTo(query);

        var res = _mapper.Map<List<TDto>>(result.Items);
        return new PagedResult<TDto>
        {
            Items = res,
            TotalCount = result.TotalCount,
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize
        };
    }
}

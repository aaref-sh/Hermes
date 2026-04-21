using AutoMapper;
using HStore.Application.DTOs;
using HStore.Application.Interfaces;
using HStore.Domain.Entities;
using HStore.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
 
using System.Linq.Expressions;

namespace HStore.Application.Services;

public abstract class BaseService<T, TDto>(IGenericRepository<T> repository, IMapper mapper) : IBaseService<TDto> where T : class, IBaseEntity where TDto : class
{
    protected readonly IGenericRepository<T> _repository = repository;
    protected readonly IMapper _mapper = mapper;

    public virtual async Task<PagedResult<TDto>> GetWithFilterAsync(FilterParams filter)
    {
        var query = _repository.GetAllAsync();

        // Filter by Name if provided (assumes T has Name property)
        if (!string.IsNullOrWhiteSpace(filter.Name) && query.Any(x => EF.Property<string>(x, "Name") != null))
        {
            query = query.Where(x => EF.Functions.Like(EF.Property<string>(x, "Name").ToLower(), $"%{filter.Name.ToLower()}%"));
        }

        var totalCount = await query.CountAsync();

        // Default sort by Name ASC
        query = query.OrderBy(x => EF.Property<string>(x, "Name"));

        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var dtoItems = _mapper.Map<IEnumerable<TDto>>(items);

        return new PagedResult<TDto>
        {
            Items = dtoItems,
            CurrentPage = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }
}

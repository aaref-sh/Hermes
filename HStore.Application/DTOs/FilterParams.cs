using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using HStore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;


namespace HStore.Application.DTOs;

public class FilterParams
{
    public string? Search { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
    public int PageNumber { get; set; } = 1;
    
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 20;
}


#region DTOs

public class RequestFilterDto
{
    public required string Name { get; set; }
    public object? Value { get; set; }
    public required string Operation { get; set; }
}

public class RequestOrderDto
{
    public required string Name { get; set; }

    [Range(1, 2)]
    public int Direction { get; set; } // 1 = Asc, 2 = Desc
}

public enum SortDirection
{
    Asc = 1,
    Desc = 2
}

#endregion

#region ModelBinders

public class JsonListModelBinder<T> : IModelBinder
{
    private static readonly JsonSerializerOptions options =
        new() { PropertyNameCaseInsensitive = true };

    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);

        if (value == ValueProviderResult.None)
            return Task.CompletedTask;

        try
        {
            var result = JsonSerializer.Deserialize<List<T>>(value.ToString(), options);
            context.Result = ModelBindingResult.Success(result ?? new List<T>());
        }
        catch
        {
            context.ModelState.AddModelError(context.ModelName, "Invalid JSON format");
        }

        return Task.CompletedTask;
    }
}

#endregion

#region Core Filter

public class FilterParams<TEntity> where TEntity : class
{
    [ModelBinder(BinderType = typeof(JsonListModelBinder<RequestFilterDto>))]
    public List<RequestFilterDto> Filters { get; set; } = new();

    [ModelBinder(BinderType = typeof(JsonListModelBinder<RequestOrderDto>))]
    public List<RequestOrderDto> Orders { get; set; } = new();

    public string? Search { get; set; }

    [Range(1, 1000)]
    public int Page { get; set; } = 1;

    [Range(1, 1000)]
    public int PerPage { get; set; } = 20;

    public bool GetAll { get; set; }

    public IQueryable<TEntity> Apply(IQueryable<TEntity> query)
    {
        query = ApplyFilters(query);
        query = ApplyOrdering(query);

        if (!GetAll)
            query = query.Skip((Page - 1) * PerPage)
                         .Take(PerPage);

        return query;
    }

    #endregion

    #region Filtering

    private IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query)
    {
        foreach (var filter in Filters)
        {
            var expression = BuildExpression(filter);
            if (expression != null)
                query = query.Where(expression);
        }

        return query;
    }

    private Expression<Func<TEntity, bool>>? BuildExpression(RequestFilterDto filter)
    {
        var property = typeof(TEntity).GetProperty(
            filter.Name,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (property == null)
            return null;

        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var member = Expression.Property(parameter, property);

        var constant = ConvertValue(filter.Value, property.PropertyType);
        if (constant == null && filter.Value != null)
            return null;

        var constantExp = Expression.Constant(constant, property.PropertyType);

        Expression body = filter.Operation.ToLower() switch
        {
            "eq" => Expression.Equal(member, constantExp),
            "ne" => Expression.NotEqual(member, constantExp),
            "gt" => Expression.GreaterThan(member, constantExp),
            "lt" => Expression.LessThan(member, constantExp),
            "gte" => Expression.GreaterThanOrEqual(member, constantExp),
            "lte" => Expression.LessThanOrEqual(member, constantExp),
            "contains" when property.PropertyType == typeof(string) =>
                Expression.Call(member, nameof(string.Contains), null, constantExp),

            "in" => BuildInExpression(member, filter.Value, property.PropertyType),

            _ => throw new ArgumentException($"Unsupported operation: {filter.Operation}")
        };

        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    private static object? ConvertValue(object? value, Type type)
    {
        if (value == null) return null;

        var targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (targetType.IsEnum)
            return Enum.Parse(targetType, value.ToString()!);

        return Convert.ChangeType(value, targetType);
    }

    private static Expression BuildInExpression(
        MemberExpression member,
        object? value,
        Type type)
    {
        if (value is not JsonElement json || json.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("IN requires array");

        var list = json.EnumerateArray()
                       .Select(x => Convert.ChangeType(x.ToString(), type))
                       .ToList();

        var constant = Expression.Constant(list);
        var method = typeof(List<>)
            .MakeGenericType(type)
            .GetMethod("Contains");

        return Expression.Call(constant, method!, member);
    }

    #endregion

    #region Ordering

    private IQueryable<TEntity> ApplyOrdering(IQueryable<TEntity> query)
    {
        if (Orders.Count == 0)
            return query;

        IOrderedQueryable<TEntity>? ordered = null;

        foreach (var order in Orders)
        {
            var property = typeof(TEntity).GetProperty(
                order.Name,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
                continue;

            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var member = Expression.Property(parameter, property);

            var lambda = Expression.Lambda(member, parameter);

            string method = (ordered == null)
                ? (order.Direction == 1 ? "OrderBy" : "OrderByDescending")
                : (order.Direction == 1 ? "ThenBy" : "ThenByDescending");

            query = ordered = (IOrderedQueryable<TEntity>)typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == method && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TEntity), property.PropertyType)
                .Invoke(null, new object[] { query, lambda })!;
        }

        return ordered ?? query;
    }

    #endregion
}




public class RequestOrdersDto
{
    public required string Name { get; set; }
    [Range(1, 2, ErrorMessage = "1 for Asc, 2 for Desc")]
    public required int Direction { get; set; }
}



// --- Model Binders (Allows JSON strings in Query Params) ---

public class RequestJsonBinder<T> : IModelBinder
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
        if (string.IsNullOrEmpty(value)) return Task.CompletedTask;

        try
        {
            var result = JsonSerializer.Deserialize<T>(value, _options);
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch { /* Log error or ignore empty results */ }

        return Task.CompletedTask;
    }
}

public class BaseFilter<TEntity> where TEntity : class, IBaseEntity
{
    [ModelBinder(BinderType = typeof(RequestJsonBinder<List<RequestFilterDto>>))]
    public List<RequestFilterDto> RequestFilters { get; set; } = [];

    [ModelBinder(BinderType = typeof(RequestJsonBinder<List<RequestOrdersDto>>))]
    public List<RequestOrdersDto> RequestOrders { get; set; } = [];

    public string? Search { get; set; }
    public bool GetAll { get; set; } = false;

    [Range(1, 1000)] public int PageNumber { get; set; } = 1;
    [Range(1, 1000)] public int PageSize { get; set; } = 20;

    private string _language = "en";
    
    /// <summary>
    /// Sets the language for LocalizedProperty filtering/ordering (e.g., "en", "ar")
    /// </summary>
    public void SetLang(string lang)
    {
        _language = lang?.ToLower() ?? "en";
    }

    /// <summary>
    /// Applies filters, ordering, and pagination to an IQueryable (SQL Server compatible)
    /// </summary>
    public async Task<PagedResult<TEntity>> ApplyTo(IQueryable<TEntity> query)
    {
        // 1. Dynamic Filters
        query = ApplyDynamicFilters(query);

        // 2. Dynamic Ordering
        query = ApplyDynamicOrdering(query);

        // 3. Pagination
        int totalCount = await query.CountAsync();

        if (!GetAll)
        {
            query = query.Skip((PageNumber - 1) * PageSize).Take(PageSize);
        }

        return new(){ PageSize = PageSize, CurrentPage = PageNumber, TotalCount = totalCount, Items = await query.ToListAsync() };
    }

    private IQueryable<TEntity> ApplyDynamicFilters(IQueryable<TEntity> query)
    {
        var entityType = typeof(TEntity);

        foreach (var filter in RequestFilters)
        {
            var prop = entityType.GetProperty(filter.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) continue;

            var parameter = Expression.Parameter(entityType, "e");
            
            // Handle LocalizedProperty type - access .En or .Ar based on language
            Expression propertyAccess;
            if (prop.PropertyType.Name == "LocalizedProperty")
            {
                var localizedProp = Expression.Property(parameter, prop);
                var langProp = _language == "ar" ? "Ar" : "En";
                propertyAccess = Expression.Property(localizedProp, langProp);
            }
            else
            {
                propertyAccess = Expression.Property(parameter, prop);
            }

            // For LocalizedProperty, use string type for comparison
            var propType = prop.PropertyType.Name == "LocalizedProperty" ? typeof(string) : prop.PropertyType;
            Expression? comparison = GetComparisonExpression(propertyAccess, prop, filter.Value?.ToString(), filter.Operation.ToLower(), propType);

            if (comparison != null)
            {
                var lambda = Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
                query = query.Where(lambda);
            }
        }
        return query;
    }

    private Expression? GetComparisonExpression(Expression propAccess, PropertyInfo propInfo, string? rawValue, string op, Type propType)
    {
        try
        {
            // Handle List-based operations (IN, NOT IN)
            if (op is "in" or "nin" or "notin")
            {
                var values = rawValue?.Trim('[', ']').Split(',').Select(v => Convert.ChangeType(v.Trim(), propType)).ToList();
                if (values == null) return null;

                var listType = typeof(List<>).MakeGenericType(propType);
                var containsMethod = listType.GetMethod("Contains");
                var listConstant = Expression.Constant(values);

                var expr = Expression.Call(listConstant, containsMethod!, propAccess);
                return (op == "in") ? expr : Expression.Not(expr);
            }

            // Handle Scalar operations
            object? convertedValue = null;
            var targetType = Nullable.GetUnderlyingType(propType) ?? propType;

            if (rawValue != null)
                convertedValue = targetType.IsEnum ? Enum.Parse(targetType, rawValue) : Convert.ChangeType(rawValue, targetType);

            var constant = Expression.Constant(convertedValue, propType);

            return op switch
            {
                "eq" or "equal" => Expression.Equal(propAccess, constant),
                "ne" or "notequal" => Expression.NotEqual(propAccess, constant),
                "gt" or "greaterthan" => Expression.GreaterThan(propAccess, constant),
                "lt" or "lessthan" => Expression.LessThan(propAccess, constant),
                "gte" or "greaterthanequal" => Expression.GreaterThanOrEqual(propAccess, constant),
                "lte" or "lessthanequal" => Expression.LessThanOrEqual(propAccess, constant),
                "contains" => Expression.Call(propAccess, typeof(string).GetMethod("Contains", [typeof(string)])!, Expression.Constant(rawValue)),
                _ => null
            };
        }
        catch { return null; }
    }

    private IQueryable<TEntity> ApplyDynamicOrdering(IQueryable<TEntity> query)
    {
        bool isFirstOrder = true;
        var entityType = typeof(TEntity);

        if (RequestOrders.Count == 0)
            RequestOrders.Add(new RequestOrdersDto { Name = "Id", Direction = 2 }); // Default Descending

        foreach (var order in RequestOrders)
        {
            var prop = entityType.GetProperty(order.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) continue;

            var parameter = Expression.Parameter(entityType, "o");
            
            // Handle LocalizedProperty type - access .En or .Ar based on language
            Expression propertyAccess;
            if (prop.PropertyType.Name == "LocalizedProperty")
            {
                var localizedProp = Expression.Property(parameter, prop);
                var langProp = _language == "ar" ? "Ar" : "En";
                propertyAccess = Expression.Property(localizedProp, langProp);
            }
            else
            {
                propertyAccess = Expression.Property(parameter, prop);
            }
            
            var conversion = Expression.Convert(propertyAccess, typeof(object));
            var lambda = Expression.Lambda<Func<TEntity, object>>(conversion, parameter);

            if (isFirstOrder)
            {
                query = order.Direction == 1 ? query.OrderBy(lambda) : query.OrderByDescending(lambda);
                isFirstOrder = false;
            }
            else
            {
                var orderedQuery = (IOrderedQueryable<TEntity>)query;
                query = order.Direction == 1 ? orderedQuery.ThenBy(lambda) : orderedQuery.ThenByDescending(lambda);
            }
        }
        return query;
    }
}
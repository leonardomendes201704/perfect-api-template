using Microsoft.EntityFrameworkCore;
using PerfectApiTemplate.Application.Abstractions;
using PerfectApiTemplate.Domain.Entities;
using PerfectApiTemplate.Infrastructure.Persistence;

namespace PerfectApiTemplate.Infrastructure.Persistence;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CustomerRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .AnyAsync(c => c.Email == email, cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListAsync(int pageNumber, int pageSize, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListByCreatedAtCursorAsync(int pageSize, string orderDir, DateTime cursorTime, Guid cursorId, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (string.Equals(orderDir, "asc", StringComparison.OrdinalIgnoreCase))
        {
            query = query
                .Where(c => c.CreatedAtUtc > cursorTime || (c.CreatedAtUtc == cursorTime && c.Id.CompareTo(cursorId) > 0))
                .OrderBy(c => c.CreatedAtUtc)
                .ThenBy(c => c.Id);
        }
        else
        {
            query = query
                .Where(c => c.CreatedAtUtc < cursorTime || (c.CreatedAtUtc == cursorTime && c.Id.CompareTo(cursorId) < 0))
                .OrderByDescending(c => c.CreatedAtUtc)
                .ThenByDescending(c => c.Id);
        }

        return await query.Take(pageSize).ToListAsync(cancellationToken);
    }
}

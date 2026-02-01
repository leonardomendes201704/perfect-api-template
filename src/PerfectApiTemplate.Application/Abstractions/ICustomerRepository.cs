using PerfectApiTemplate.Domain.Entities;

namespace PerfectApiTemplate.Application.Abstractions;

public interface ICustomerRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> ListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}


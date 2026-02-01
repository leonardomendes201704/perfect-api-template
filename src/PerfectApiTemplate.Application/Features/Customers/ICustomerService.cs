using PerfectApiTemplate.Application.Common.Models;

namespace PerfectApiTemplate.Application.Features.Customers;

public interface ICustomerService
{
    Task<RequestResult<CustomerDto>> CreateCustomerAsync(string name, string email, DateOnly dateOfBirth, CancellationToken cancellationToken);
}


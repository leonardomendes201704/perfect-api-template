using MediatR;
using PerfectApiTemplate.Application.Abstractions;
using PerfectApiTemplate.Application.Common.Models;

namespace PerfectApiTemplate.Application.Features.Customers.Queries;

public sealed class ListCustomersQueryHandler : IRequestHandler<ListCustomersQuery, RequestResult<IReadOnlyList<CustomerDto>>>
{
    private const int MaxPageSize = 100;
    private readonly ICustomerRepository _customerRepository;

    public ListCustomersQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<RequestResult<IReadOnlyList<CustomerDto>>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize is < 1 or > MaxPageSize ? 20 : request.PageSize;

        var customers = await _customerRepository.ListAsync(pageNumber, pageSize, cancellationToken);
        var dtos = customers.Select(c => new CustomerDto(c.Id, c.Name, c.Email, c.DateOfBirth, c.CreatedAtUtc)).ToList();
        return RequestResult<IReadOnlyList<CustomerDto>>.Success(dtos);
    }
}

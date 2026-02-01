using MediatR;
using PerfectApiTemplate.Application.Common.Models;

namespace PerfectApiTemplate.Application.Features.Customers.Queries;

public sealed record ListCustomersQuery(int PageNumber = 1, int PageSize = 20) : IRequest<RequestResult<IReadOnlyList<CustomerDto>>>;

using MediatR;
using PerfectApiTemplate.Application.Common.Models;
using PerfectApiTemplate.Application.Common.Pagination;

namespace PerfectApiTemplate.Application.Features.Customers.Queries;

public sealed record ListCustomersQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string OrderBy = "CreatedAtUtc",
    string OrderDir = "desc",
    string? Cursor = null,
    bool IncludeInactive = false) : IRequest<RequestResult<CursorPaginationResult<CustomerDto>>>;

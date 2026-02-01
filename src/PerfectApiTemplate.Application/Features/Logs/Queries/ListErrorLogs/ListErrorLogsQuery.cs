using MediatR;
using PerfectApiTemplate.Application.Common.Models;
using PerfectApiTemplate.Application.Common.Pagination;

namespace PerfectApiTemplate.Application.Features.Logs.Queries.ListErrorLogs;

public sealed record ListErrorLogsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string OrderBy = "CreatedAtUtc",
    string OrderDir = "desc",
    string? ExceptionType = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null) : IRequest<RequestResult<PagedResult<ErrorLogDto>>>;


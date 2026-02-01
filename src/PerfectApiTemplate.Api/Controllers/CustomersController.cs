using MediatR;
using Microsoft.AspNetCore.Mvc;
using PerfectApiTemplate.Api.Contracts;
using PerfectApiTemplate.Api.Extensions;
using PerfectApiTemplate.Application.Features.Customers.Commands;
using PerfectApiTemplate.Application.Features.Customers.Queries;

namespace PerfectApiTemplate.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateCustomerCommand(request.Name, request.Email, request.DateOfBirth), cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return CreatedAtAction(nameof(GetCustomerById), new { id = result.Value.Id }, result.Value);
        }

        return this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomerById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ListCustomers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ListCustomersQuery(pageNumber, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }
}


namespace PerfectApiTemplate.Api.Contracts;

public sealed record CreateCustomerRequest(string Name, string Email, DateOnly DateOfBirth);


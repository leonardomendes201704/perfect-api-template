# agent.md - DemoMvc Controllers

## Purpose
- MVC controllers for routing, binding, and orchestration of UI flows.

## Allowed dependencies
- ViewModels, ApiClients, Infrastructure helpers.

## Mandatory patterns
- Thin controllers: call ApiClients only.
- Use PRG (redirect after POST).
- Set TempData for success/error feedback.

## Forbidden actions
- Business logic in controllers.
- Direct HttpClient usage (must use ApiClients).

## Acceptance checklist
- Input validation handled with ModelState.
- Redirect after successful POST.
- EN-US naming.

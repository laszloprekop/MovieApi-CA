# Errors via domain exceptions + IExceptionHandler

Once business logic moves into the service layer it can no longer call `NotFound()` / `BadRequest()`.
We signal failure by **throwing a small domain exception hierarchy** (e.g. `NotFoundException`,
`BusinessRuleException`, validation errors) and mapping them centrally to status codes +
`ProblemDetails` in a single `IExceptionHandler` registered via `AddProblemDetails`.

Considered and rejected: a `Result<T>` return type. Result is more explicit and avoids
exceptions-as-control-flow, but wraps every service method and pushes status-code mapping into each
controller.

**Why exceptions:** service signatures stay honest (`Task<MovieDto>`, not `Task<Result<MovieDto>>`),
controllers stay thin, and error→ProblemDetails mapping lives in exactly one place. The cost we
accept is using exceptions for non-exceptional rule violations.

**Note:** Del 9 invites demonstrating both styles. If we do, exceptions+middleware is the house
default and any `Result<T>` controller is a deliberate one-off to show the alternative.

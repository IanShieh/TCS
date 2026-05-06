<!--
Sync Impact Report — Constitution Amendment v1.1.0 → v1.2.0
==============================================================
Date: 2026-03-16
Change Type: MINOR (Translated constitution to English for spec-kit consumption)

Modified Principles: None (All I–VI preserved; language changed to English)

Added Sections: None

Removed Sections: None

Templates Requiring Updates:
✅ plan-template.md — No change needed (templates remain zh-TW)
✅ spec-template.md — No change needed (templates remain zh-TW)
✅ tasks-template.md — No change needed (templates remain zh-TW)

Follow-up TODOs: None
-->

# Dingxin ERP Operation Conversion Project — Team Constitution

## Core Principles

### I. Modular Architecture

All projects must adopt a **Clean Architecture** three-layer structure. Each
layer is an independent, self-contained module with clear separation of
responsibilities. Modules must be independently testable, fully documented,
and reusable across different operations. Strict separation of concerns
ensures maintainability and reduces coupling between business logic,
validation logic, and data persistence.

- **Web Layer** (`*.Web`): Controllers, Views, Middleware, wwwroot
- **Core Layer** (`*.Core`): Entities, DTOs, Interfaces, Services,
  Validators, Common
- **Infrastructure Layer** (`*.Infrastructure`): DbContext,
  Configurations, Repositories

Dependency direction: Web → Core ← Infrastructure
(Core has no external dependencies except FluentValidation)

### II. Documentation-First

Every feature, API, and data structure must have complete documentation
before or during implementation. Documentation includes purpose, usage
examples, validation rules, and any business logic constraints. Code
comments explain "why" not "what" — ambiguous ERP requirements must be
clarified in documentation before implementation begins.

- All specs, plans, and user-facing documents **must use Traditional
  Chinese (zh-TW)**
- Code variable names and method names use **English**
- Code comments use **Traditional Chinese**
- XML Summary uses **Traditional Chinese**

### III. Configuration-Driven Development

Behavior must be controlled through configuration whenever possible,
rather than modifying code.

- Dingxin ERP `char()` columns must use
  `IsFixedLength()` + `IsUnicode(false)` configuration
- All Entity Configurations use `IEntityTypeConfiguration<T>`
- Audit fields (CREATOR/CREATE_DATE/MODIFIER/MODI_DATE/FLAG)
  managed uniformly via `IAuditableEntity`
- JSON serialization uses `PropertyNamingPolicy = null`
  (preserves PascalCase; ERP field names are not transformed)
- Auto-switch to InMemory DB demo mode when no connection string
  is configured
- Default settings must be backward compatible — new features
  default to disabled
- Configuration changes must not require code recompilation
- All configuration options must have corresponding documentation

Rationale: Configuration-driven approach reduces development time,
minimizes code changes, and allows business users to understand
system capabilities through configuration files.

### IV. Service-Oriented Architecture with Clear Boundaries

Every Service must follow the Single Responsibility Principle and
have a clearly defined interface.

- **Controllers** handle only routing and Request/Response
- **Services** contain business logic, validation, and data
  coordination (implemented in Core layer)
- **Repositories** handle data access (implemented in
  Infrastructure layer)
- Unified return format: `CrudResult<T>`
  (Success/Message/Data/Errors)
- Pagination format: `PagedResult<T>`
  (Items/TotalItems/TotalPages/CurrentPage/PageSize)
- Services must depend on abstractions (interfaces), never on
  concrete implementations
- Service registration must use DI (`AddScoped<IService, Service>()`
  in `Program.cs`)
- Validation: FluentValidation, Validators in Core layer
- Header/Detail CRUD **fully separated**: independent API endpoints,
  Modals, and action buttons

Rationale: Clear architectural boundaries make the system testable,
maintainable, and promote team collaboration by reducing coupling.

### V. User-Centric Design

User requirements and feedback from the ERP operations team drive all
design decisions. Feature requirements must be validated with end users
before implementation. The system must support how users actually work
with ERP data, rather than imposing arbitrary workflows.

- Frontend UI adopts **traditional Web Form style**
  (search bar + table + Modal editing)
- Header-Detail linkage: click header row → AJAX loads detail table
- Header row click auto-select: checkbox checked + enable
  edit/delete buttons
- Switching rows auto-deselects previous row (single-select mode)
- Keyboard shortcuts: Ctrl+N (create), Ctrl+E (edit), Delete (delete),
  F5 (refresh)
- Toast notifications: non-blocking messages
- Bootstrap 5 + jQuery (CDN)

### VI. Mandatory Language Requirements — Non-Negotiable

All specs, plans, and user-facing documents **must use Traditional
Chinese (zh-TW)**.

- Feature specs in `/specs/` must use Traditional Chinese
- Implementation plans (plan.md) must use Traditional Chinese
- User stories and acceptance criteria must use Traditional Chinese
- Task lists (tasks.md) must use Traditional Chinese
- README must use Traditional Chinese
- User-facing error messages must use Traditional Chinese
- Entity/DTO property names: use ERP original field names
  (e.g., TA001, TB003)
- Method/class names: **English**
  (e.g., `GetByKeyAsync`, `SampleService`)
- Git Commit Messages: **English** (Conventional Commits)

Rationale: Language consistency ensures all stakeholders (including
non-English speakers) can fully understand and participate in project
documentation and development decisions.

## Code Quality Standards

### Naming Conventions

- **Traditional Chinese comments required**: for business logic
  explanations
- **English required**: for code identifiers (classes, methods,
  variables)
- Controller naming pattern: `{Feature}Controller.cs`
- Service naming pattern: `I{Service}Service.cs` (interface),
  `{Service}Service.cs` (implementation)
- Repository naming pattern: `I{Feature}Repository.cs` (interface),
  `{Feature}Repository.cs` (implementation)
- API endpoints must follow REST conventions:
  `/api/{resource}` (header CRUD)
  `/api/{resource}/{pk1}/{pk2}/details` (detail CRUD)
- Method names must be descriptive and start with a verb:
  `GetByKeyAsync`, `CreateAsync`, `DeleteDetailAsync`

### Code Organization

- Use XML Summary (`/// <summary>`) to describe API method purposes
- Entity fields use XML Summary to annotate Chinese field descriptions
- API methods ordered: Header CRUD → Detail CRUD
- Maximum method length: 50 lines (exceptions require justification)
- Maximum class length: 1000 lines (must refactor if exceeded)

### Error Handling

- All Service methods must return `CrudResult<T>` with
  success/failure status
- Exceptions must be caught and wrapped as
  `CrudResult.ErrorResult(message, errors)`
- User-facing error messages must use Traditional Chinese
- Internal error details must be logged with `ILogger` including
  stack traces
- Global exceptions handled uniformly via
  `ExceptionHandlingMiddleware`
- HTTP status codes must follow standards:
  200 (success), 201 (created), 400 (validation failure),
  404 (not found), 500 (server error)

## Security Requirements

ERP data requires strict security controls:

### Secrets Management

- **Strictly prohibited** to hard-code API keys, tokens, or
  passwords in source code
- All secrets must be stored in environment variables or secret
  managers (e.g., Azure Key Vault)
- `.env` / `.env.local` files must be listed in `.gitignore` and
  **must never** be committed to version control
- Git history must not contain secrets; any accidental commits
  require immediate key rotation
- Production secrets must be configured on the hosting platform
  (e.g., Azure App Service application settings)
- Application must fail fast at startup if required secrets are
  missing: throw descriptive error before processing any requests

Rationale: Leaked credentials are the most common cause of data
breaches. Secrets must be treated as an infrastructure concern,
not a code concern.

### Input Validation & Sanitization

- All user input must be validated via FluentValidation
- SQL Injection protection: use EF Core parameterized queries
  (mandatory)
- XSS protection: user-provided text must be HTML-sanitized
- Dangerous SQL patterns must be rejected:
  `--, ;, /*, */, EXEC, DROP, ALTER`
- File uploads must validate file type and size

### API Security

- Production API endpoints must have proper authorization protection
- CSRF protection: enable anti-forgery tokens for state-changing
  operations
- CORS configuration: allow only explicitly whitelisted origins
- Rate limiting: prevent brute-force attacks and DDoS

### Sensitive Data Exposure Prevention

- Passwords, tokens, API keys, and PII **must never** appear in
  application logs
- Log entries must mask sensitive fields; use identifiers
  (e.g., `userId`, `last4`) instead of raw values
- User-facing error responses may only return generic messages;
  stack traces and internal error details **must not** be exposed
- Detailed error information may only be recorded in server-side
  logs with appropriate access controls
- HTTP responses must not include server version headers that aid
  fingerprinting (`Server`, `X-Powered-By`)

Rationale: Accidental exposure of sensitive data in logs or error
responses is a leading cause of credential theft and privacy breaches.

## Performance & Optimization Standards

- Database queries use `Select` to project specific columns; avoid
  loading all fields of an entire Entity
- Read-only queries use `AsNoTracking()` for better performance
- Implement pagination: tables exceeding 100 records must be paginated
- Batch database operations: use batch operations when processing
  multiple records
- All I/O operations use `async/await` asynchronous pattern
- Include related data: only Include details when actually needed
- Frontend tables: auto-pagination + search to avoid loading
  excessive data

## Development Workflow

- **SDD Process**: Use spec-kit driven development
  (`/speckit.specify → /speckit.plan → /speckit.tasks
  → /speckit.implement → /speckit.checklist`)
- **Code Reviews**: All ERP business logic changes require peer
  review before merging to main
- **Testing Requirements**: All CRUD operations and validation rules
  require integration tests
- **Documentation Standards**: Documentation updates must be
  synchronized with code changes
- **Release Process**: Semantic versioning; breaking changes to
  Entity structures require major version increment
- **Build Verification**: `dotnet build` with zero errors and zero
  warnings before committing

## Governance

This constitution supersedes all other development conventions in this
project. All Pull Requests must be verified for compliance with these
principles. When these principles conflict with other guidelines, the
constitution takes precedence.

Development decisions should be argued based on these core principles.
The constitution may only be amended when the project's fundamental
direction changes fundamentally; amendments require documented rationale
and a plan to update dependent documents (plan.md, spec.md, tasks.md).

### Living Documentation

- The constitution is a living document — continuous improvement is
  necessary
- Feedback from implementation experience should be considered for
  revisions
- Annual review recommended to ensure principles remain applicable
- Use `.specify/memory/constitution.md` as the single source of truth
  for the constitution
- All template files (plan, spec, tasks) must align with constitutional
  principles

### Compliance Verification

- All Code Reviews must verify compliance with this constitution
- Pull Requests violating principles must be rejected with
  constitutional citations
- Exceptions to principles must be documented in the implementation
  plan's complexity tracking section
- Complexity must be justified: "why it is needed" and
  "why simpler alternatives were rejected"

**Version**: 1.2.0 | **Ratified**: 2026-03-15 | **Last Amended**: 2026-03-16

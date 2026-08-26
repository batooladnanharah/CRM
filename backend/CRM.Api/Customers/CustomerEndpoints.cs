using System.ComponentModel.DataAnnotations;
using CRM.Api.Auth;
using CRM.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Customers;

public static class CustomerEndpoints
{
    private static readonly string[] AllowedSortColumns = ["fullName", "email", "company", "createdAtUtc"];

    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var customers = app.MapGroup("/api/customers");

        customers.MapGet("/", async ([AsParameters] CustomerListQuery query, CustomerDbContext db) =>
        {
            string sortBy;
            if (string.IsNullOrWhiteSpace(query.SortBy))
            {
                sortBy = "createdAtUtc";
            }
            else if (!AllowedSortColumns.Contains(query.SortBy))
            {
                return Results.BadRequest(new ErrorResponse($"Invalid sortBy value: '{query.SortBy}'."));
            }
            else
            {
                sortBy = query.SortBy;
            }

            string sortDir;
            if (string.IsNullOrWhiteSpace(query.SortDir))
            {
                sortDir = "asc";
            }
            else if (query.SortDir is not ("asc" or "desc"))
            {
                return Results.BadRequest(new ErrorResponse($"Invalid sortDir value: '{query.SortDir}'."));
            }
            else
            {
                sortDir = query.SortDir;
            }

            // Page/PageSize are clamped, never rejected — there is no sensible
            // "invalid value" response for an out-of-range page number, and the
            // effective (post-clamp) values are always returned in the response.
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            IQueryable<Customer> filtered = db.Customers;

            // Normalized, provider-agnostic search: translates correctly on both
            // Npgsql and the EF Core InMemory test provider, and EF Core's SQL
            // generation for .Contains() parameterizes/escapes automatically —
            // unlike a raw ILike pattern, a literal '%'/'_' in the search term is
            // treated as a literal substring, not a wildcard.
            var term = query.Search?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(term))
            {
                filtered = filtered.Where(c =>
                    c.FullName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term) ||
                    (c.Phone != null && c.Phone.ToLower().Contains(term)));
            }

            var company = query.Company?.Trim();
            if (!string.IsNullOrEmpty(company))
            {
                filtered = filtered.Where(c => c.Company == company);
            }

            var descending = sortDir == "desc";
            filtered = sortBy switch
            {
                "fullName" => descending
                    ? filtered.OrderByDescending(c => c.FullName)
                    : filtered.OrderBy(c => c.FullName),
                "email" => descending
                    ? filtered.OrderByDescending(c => c.Email)
                    : filtered.OrderBy(c => c.Email),
                "company" => descending
                    ? filtered.OrderByDescending(c => c.Company)
                    : filtered.OrderBy(c => c.Company),
                _ => descending
                    ? filtered.OrderByDescending(c => c.CreatedAtUtc)
                    : filtered.OrderBy(c => c.CreatedAtUtc),
            };

            var totalCount = await filtered.CountAsync();

            var items = await filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerListItem(c.Id, c.FullName, c.Email, c.Phone, c.Company, c.CreatedAtUtc))
                .ToListAsync();

            return Results.Ok(new PagedResult<CustomerListItem>(items, page, pageSize, totalCount));
        })
        .RequireAuthorization()
        .WithName("ListCustomers")
        .WithTags("Customers")
        .Produces<PagedResult<CustomerListItem>>();

        customers.MapPost("/", async (CreateCustomerRequest request, CustomerDbContext db, IAuditLogger auditLogger) =>
        {
            var fullName = request.FullName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(fullName))
            {
                return Results.BadRequest(new ErrorResponse("Full name is required."));
            }
            if (fullName.Length > 200)
            {
                return Results.BadRequest(new ErrorResponse("Full name must be 200 characters or fewer."));
            }

            var email = request.Email?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(email))
            {
                return Results.BadRequest(new ErrorResponse("Email is required."));
            }
            if (!new EmailAddressAttribute().IsValid(email))
            {
                return Results.BadRequest(new ErrorResponse("Invalid email format."));
            }
            if (email.Length > 320)
            {
                return Results.BadRequest(new ErrorResponse("Email must be 320 characters or fewer."));
            }

            if (request.Phone is { Length: > 32 })
            {
                return Results.BadRequest(new ErrorResponse("Phone must be 32 characters or fewer."));
            }
            if (request.Company is { Length: > 200 })
            {
                return Results.BadRequest(new ErrorResponse("Company must be 200 characters or fewer."));
            }

            var normalizedEmail = email.ToLowerInvariant();

            if (await db.Customers.AnyAsync(c => c.Email == normalizedEmail))
            {
                return Results.Json(new ErrorResponse("A customer with this email already exists."),
                    statusCode: StatusCodes.Status409Conflict);
            }

            var entity = new Customer
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Email = normalizedEmail,
                Phone = request.Phone,
                Company = request.Company,
                CreatedAtUtc = DateTime.UtcNow,
            };

            try
            {
                db.Customers.Add(entity);
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Race-safety net: the pre-check above can miss a concurrent insert;
                // the unique index on Email is the source of truth.
                return Results.Json(new ErrorResponse("A customer with this email already exists."),
                    statusCode: StatusCodes.Status409Conflict);
            }

            await auditLogger.WriteAsync(
                AuditActions.CustomerCreated, targetType: "customer", targetId: entity.Id.ToString());

            var response = new CustomerListItem(entity.Id, entity.FullName, entity.Email, entity.Phone, entity.Company, entity.CreatedAtUtc);
            return Results.Created($"/api/customers/{entity.Id}", response);
        })
        .RequireAuthorization(Permissions.CustomersManage)
        .WithName("CreateCustomer")
        .WithTags("Customers")
        .Produces<CustomerListItem>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        customers.MapGet("/{id:guid}", async (Guid id, CustomerDbContext db) =>
        {
            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (customer is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new CustomerListItem(
                customer.Id, customer.FullName, customer.Email, customer.Phone, customer.Company, customer.CreatedAtUtc));
        })
        .RequireAuthorization()
        .WithName("GetCustomer")
        .WithTags("Customers")
        .Produces<CustomerListItem>()
        .Produces(StatusCodes.Status404NotFound);

        customers.MapPut("/{id:guid}", async (
            Guid id, UpdateCustomerRequest request, CustomerDbContext db, IAuditLogger auditLogger) =>
        {
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id);
            if (customer is null)
            {
                return Results.NotFound();
            }

            var fullName = request.FullName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(fullName))
            {
                return Results.BadRequest(new ErrorResponse("Full name is required."));
            }
            if (fullName.Length > 200)
            {
                return Results.BadRequest(new ErrorResponse("Full name must be 200 characters or fewer."));
            }

            var email = request.Email?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(email))
            {
                return Results.BadRequest(new ErrorResponse("Email is required."));
            }
            if (!new EmailAddressAttribute().IsValid(email))
            {
                return Results.BadRequest(new ErrorResponse("Invalid email format."));
            }
            if (email.Length > 320)
            {
                return Results.BadRequest(new ErrorResponse("Email must be 320 characters or fewer."));
            }

            if (request.Phone is { Length: > 32 })
            {
                return Results.BadRequest(new ErrorResponse("Phone must be 32 characters or fewer."));
            }
            if (request.Company is { Length: > 200 })
            {
                return Results.BadRequest(new ErrorResponse("Company must be 200 characters or fewer."));
            }

            var normalizedEmail = email.ToLowerInvariant();

            // Excludes the row being edited, unlike the create-time duplicate check.
            if (await db.Customers.AnyAsync(c => c.Id != id && c.Email == normalizedEmail))
            {
                return Results.Json(new ErrorResponse("A customer with this email already exists."),
                    statusCode: StatusCodes.Status409Conflict);
            }

            customer.FullName = fullName;
            customer.Email = normalizedEmail;
            customer.Phone = request.Phone;
            customer.Company = request.Company;
            customer.UpdatedAtUtc = DateTime.UtcNow;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Race-safety net: the pre-check above can miss a concurrent update;
                // the unique index on Email is the source of truth.
                return Results.Json(new ErrorResponse("A customer with this email already exists."),
                    statusCode: StatusCodes.Status409Conflict);
            }

            await auditLogger.WriteAsync(
                AuditActions.CustomerUpdated, targetType: "customer", targetId: customer.Id.ToString());

            var response = new CustomerListItem(
                customer.Id, customer.FullName, customer.Email, customer.Phone, customer.Company, customer.CreatedAtUtc);
            return Results.Ok(response);
        })
        .RequireAuthorization(Permissions.CustomersManage)
        .WithName("UpdateCustomer")
        .WithTags("Customers")
        .Produces<CustomerListItem>()
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        customers.MapGet("/{id:guid}/interactions", async (
            Guid id, [AsParameters] CustomerInteractionsQuery query, CustomerDbContext db) =>
        {
            var customerExists = await db.Customers.AsNoTracking().AnyAsync(c => c.Id == id);
            if (!customerExists)
            {
                return Results.NotFound();
            }

            // Clamped, never rejected — same convention as the customer list endpoint.
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var interactionsQuery = db.CustomerInteractions
                .AsNoTracking()
                .Where(i => i.CustomerId == id);

            var totalCount = await interactionsQuery.CountAsync();

            // Secondary sort by Id gives a stable, deterministic order when
            // multiple rows share the same OccurredAt value.
            var entities = await interactionsQuery
                .OrderByDescending(i => i.OccurredAt)
                .ThenByDescending(i => i.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = entities
                .Select(i => new CustomerInteractionDto(
                    i.Id, i.Type.ToString(), i.Summary, i.OccurredAt, i.ActorName, i.ActorId, i.TicketId))
                .ToList();

            return Results.Ok(new PagedResult<CustomerInteractionDto>(items, page, pageSize, totalCount));
        })
        .RequireAuthorization()
        .WithName("GetCustomerInteractions")
        .WithTags("Customers")
        .Produces<PagedResult<CustomerInteractionDto>>()
        .Produces(StatusCodes.Status404NotFound);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class TicketAttachmentsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketAttachmentsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(
        string email = CustomWebApplicationFactory.ActiveEmail,
        string password = CustomWebApplicationFactory.ActivePassword)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid CreateCustomer(string fullName, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer.Id;
    }

    private Guid CreateTicket(Guid customerId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "Sample ticket",
            Description = "Sample description",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
    }

    private int HistoryCount(Guid ticketId, TicketChangeType changeType)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.TicketHistory.Count(h => h.TicketId == ticketId && h.ChangeType == changeType);
    }

    private static MultipartFormDataContent BuildUpload(
        string fileName, string contentType, byte[]? bytes = null)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes ?? Encoding.UTF8.GetBytes("sample file content"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    [Fact]
    public async Task Post_Attachment_UploadsFile_ReturnsCreated_AndWritesHistory()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Attachment Customer", "attachment.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsync(
            $"/api/tickets/{ticketId}/attachments", BuildUpload("notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var attachment = await response.Content.ReadFromJsonAsync<TicketAttachmentResponse>();
        Assert.Equal(ticketId, attachment!.TicketId);
        Assert.Equal("notes.txt", attachment.OriginalFileName);
        Assert.Equal("Active Agent", attachment.UploadedByDisplayName);
        Assert.Equal(1, HistoryCount(ticketId, TicketChangeType.AttachmentAdded));
    }

    [Fact]
    public async Task Get_Attachments_ReturnsUploadedFile()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("List Attachment Customer", "list.attachment.customer@example.com");
        var ticketId = CreateTicket(customerId);
        var upload = await client.PostAsync(
            $"/api/tickets/{ticketId}/attachments", BuildUpload("report.txt", "text/plain"));
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        var response = await client.GetAsync($"/api/tickets/{ticketId}/attachments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var attachments = await response.Content.ReadFromJsonAsync<List<TicketAttachmentResponse>>();
        Assert.Single(attachments!);
        Assert.Equal("report.txt", attachments![0].OriginalFileName);
    }

    [Fact]
    public async Task Get_Download_StreamsOriginalContent()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Download Customer", "download.customer@example.com");
        var ticketId = CreateTicket(customerId);
        var bytes = Encoding.UTF8.GetBytes("the actual file bytes");
        var upload = await client.PostAsync(
            $"/api/tickets/{ticketId}/attachments", BuildUpload("data.txt", "text/plain", bytes));
        var attachment = await upload.Content.ReadFromJsonAsync<TicketAttachmentResponse>();

        var response = await client.GetAsync($"/api/tickets/{ticketId}/attachments/{attachment!.Id}/download");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var downloaded = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(bytes, downloaded);
    }

    [Fact]
    public async Task Delete_Attachment_RemovesRow_AndWritesHistory()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Delete Customer", "delete.customer@example.com");
        var ticketId = CreateTicket(customerId);
        var upload = await client.PostAsync(
            $"/api/tickets/{ticketId}/attachments", BuildUpload("deleteme.txt", "text/plain"));
        var attachment = await upload.Content.ReadFromJsonAsync<TicketAttachmentResponse>();

        var response = await client.DeleteAsync($"/api/tickets/{ticketId}/attachments/{attachment!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, HistoryCount(ticketId, TicketChangeType.AttachmentRemoved));

        var list = await client.GetAsync($"/api/tickets/{ticketId}/attachments");
        var attachments = await list.Content.ReadFromJsonAsync<List<TicketAttachmentResponse>>();
        Assert.Empty(attachments!);
    }

    [Fact]
    public async Task Post_Attachment_Returns400_WhenFileTooLarge()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Too Large Customer", "too.large.customer@example.com");
        var ticketId = CreateTicket(customerId);
        var oversized = new byte[10 * 1024 * 1024 + 1];

        var response = await client.PostAsync(
            $"/api/tickets/{ticketId}/attachments", BuildUpload("big.txt", "text/plain", oversized));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Attachment_Returns400_WhenContentTypeNotAllowed()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Bad Mime Customer", "bad.mime.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsync(
            $"/api/tickets/{ticketId}/attachments", BuildUpload("script.exe", "application/x-msdownload"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Attachment_Returns404_WhenTicketMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsync(
            $"/api/tickets/{Guid.NewGuid()}/attachments", BuildUpload("notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Attachment_Returns403_ForCustomerRole()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.PostAsync(
            $"/api/tickets/{Guid.NewGuid()}/attachments", BuildUpload("notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

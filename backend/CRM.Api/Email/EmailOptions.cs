namespace CRM.Api.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Development";
    public string FromAddress { get; set; } = "no-reply@crm.local";
    public string FromName { get; set; } = "CRM";
    public SmtpOptions? Smtp { get; set; }
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
}

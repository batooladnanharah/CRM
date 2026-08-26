namespace CRM.Api.Customers.Attachments;

public sealed class AttachmentsOptions
{
    public const string SectionName = "Attachments";

    public string StorageRoot { get; set; } = "App_Data/attachments";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedContentTypes { get; set; } = [];
    public string[] AllowedExtensions { get; set; } = [];
}

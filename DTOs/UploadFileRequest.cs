namespace WorkflowBackend.DTOs
{
    public class FileUploadRequest
{
    public IFormFile File { get; set; } = default!;
    public Guid ActivityId { get; set; }
}

}

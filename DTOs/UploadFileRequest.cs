namespace WorkflowBackend.DTOs
{
    public class UploadFileRequest
    {
        public IFormFile File { get; set; } = default!;
        public string Folder { get; set; } = "uploads"; // optional
    }
}

namespace _2021p3web_gallery_PatrikMrnka.Models
{
    public class StoredFileListViewModel
    {
        public Guid Id { get; set; }
        public ApplicationUser Uploader { get; set; }
        public string UploaderId { get; set; }
        public DateTime UploadedAt { get; set; }
        public string OriginalName { get; set; }
        public string ContentType { get; set; }
        public int ThumbnailCount { get; set; }
    }
}

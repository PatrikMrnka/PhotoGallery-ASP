using _2021p3web_gallery_PatrikMrnka.Data;
using _2021p3web_gallery_PatrikMrnka.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace _2021p3web_gallery_PatrikMrnka.Pages
{
    public class IndexModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private readonly ILogger<IndexModel> _logger;
        private ApplicationDbContext _context;

        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        public List<string> Files { get; set; } = new List<string>();




        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment environment, ApplicationDbContext context)
        {
            _logger = logger;
            _environment = environment;
            _context = context;
        }

        public void OnGet()
        {
            var fullNames = Directory.GetFiles(Path.Combine(_environment.ContentRootPath, "Uploads")).ToList();
            foreach (var fn in fullNames)
            {
                Files.Add(Path.GetFileName(fn));
            }
        }
        public IActionResult OnGetDownload(string filename)
        {
            filename = Path.GetFileName(filename);
            var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", filename);
            if (System.IO.File.Exists(fullName)) // existuje soubor na disku?
            {
                var fileRecord = _context.Files.Find(Guid.Parse(filename));
                if (fileRecord != null) // je soubor v databázi?
                {
                    return PhysicalFile(fullName, fileRecord.ContentType, fileRecord.OriginalName); // vrať ho zpátky pod původním názvem a typem
                }
                else
                {
                    ErrorMessage = "Žádný záznam o souboru v databázi.";
                    return RedirectToPage();
                }
            }
            else
            //return NotFound();
            {
                ErrorMessage = "Soubor nenalezen.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnGetThumbnail(string filename, ThumbnailType type = ThumbnailType.Square)
        {
            StoredFile file = await _context.Files
              .AsNoTracking()
              .Where(f => f.Id == Guid.Parse(filename))
              .SingleOrDefaultAsync();
            if (file == null)
            {
                return NotFound("no record for this file");
            }
            Thumbnail thumbnail = await _context.Thumbnails
              .AsNoTracking()
              .Where(t => t.FileId == Guid.Parse(filename) && t.Type == type)
              .SingleOrDefaultAsync();
            if (thumbnail != null)
            {
                return File(thumbnail.Blob, file.ContentType);
            }
            return NotFound("no thumbnail for this file");
        }
    }
}
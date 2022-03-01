#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using _2021p3web_gallery_PatrikMrnka.Data;
using _2021p3web_gallery_PatrikMrnka.Models;

namespace _2021p3web_gallery_PatrikMrnka.Pages.Pictures
{
    public class IndexModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public IndexModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<StoredFile> StoredFile { get;set; }
        public IList<StoredFileListViewModel> Files { get; set; }


        public async Task OnGetAsync()
        {
            StoredFile = await _context.Files
                .Include(s => s.Uploader).ToListAsync();
            Files = _context.Files
    .AsNoTracking()
    .Include(f => f.Uploader)
    .Include(f => f.Thumbnails)
    .Select(f => new StoredFileListViewModel
    {
        Id = f.Id,
        ContentType = f.ContentType,
        OriginalName = f.OriginalName,
        UploaderId = f.UploaderId,
        Uploader = f.Uploader,
        UploadedAt = f.UploadedAt,
        ThumbnailCount = f.Thumbnails.Count
    })
    .ToList();
        }
    }
}

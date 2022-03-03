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
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace _2021p3web_gallery_PatrikMrnka.Pages.Pictures
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public IndexModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<StoredFile> StoredFile { get; set; }
        public int AlbumCount { get; set; }
        public string? userId { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
            StoredFile = await _context.Files.Include(s => s.Uploader).Where(a => a.UploaderId == userId).ToListAsync();

        }
    }
}

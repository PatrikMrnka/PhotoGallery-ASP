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

namespace _2021p3web_gallery_PatrikMrnka.Pages.Albums.Specific
{
    public class DetailsModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public DetailsModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public StoredFileAlbum StoredFileAlbum { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            StoredFileAlbum = await _context.StoredFileAlbums
                .Include(s => s.Album)
                .Include(s => s.Picture).FirstOrDefaultAsync(m => m.PictureId == id);

            if (StoredFileAlbum == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}

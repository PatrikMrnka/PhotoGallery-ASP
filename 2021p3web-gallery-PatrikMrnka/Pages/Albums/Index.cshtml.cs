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

namespace _2021p3web_gallery_PatrikMrnka.Pages.Albums
{

    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public IndexModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Album> Album { get; set; }
        public IList<Album> AlbumPublic { get; set; }
        public IList<StoredFileAlbum> StoredFileAlbum { get; set; }
        public int PicturesInAlbum { get; set; }
        public int AlbumCount { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumCount = _context.Albums.Where(a => a.UploaderId == userId).Where(a => a.Name != "Výchozí").Count(); // Počet soukromých alb mimo Výchozí album
            Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).ToListAsync(); 

            //Album = await _context.Albums
            //    .Include(a => a.Uploader).ToListAsync();
        }
    }
}

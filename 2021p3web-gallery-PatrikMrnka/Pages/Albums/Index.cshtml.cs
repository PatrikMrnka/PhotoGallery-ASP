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

        public Album Album { get; set; }
        public Guid AlbumDefaultId { get; set; }
        public string ErrorMessage { get; set; }
        [BindProperty]
        public string NewAlbumName { get; set; }
        public IList<Album> Albums { get; set; }
        public IList<Album> AlbumPublic { get; set; }
        public IList<StoredFileAlbum> StoredFileAlbum { get; set; }
        public int PicturesInAlbum { get; set; }
        public int AlbumCount { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumCount = _context.Albums.Where(a => a.UploaderId == userId).Count(); // Počet soukromých alb
            Albums = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).ToListAsync();
            AlbumDefaultId = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefaultAsync();

            //Album = await _context.Albums
            //    .Include(a => a.Uploader).ToListAsync();
        }

        public async Task<IActionResult> OnGetDelete(Guid? albumid)
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele

            if (albumid == null)
            {
                return Page();
            }
            else if (albumid == AlbumDefaultId)
            {
                ErrorMessage = "Nelze vymazat výchozí album.";
                return Page();
            }
            else
            { 
                Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                _context.Albums.Remove(Album);
                await _context.SaveChangesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCreateNewAlbum()
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele

            var newAlbum = new Album
            {
                Name = NewAlbumName,
                CreationDate = DateTime.Now,
                IsPublic = false,
                UploaderId = userId
            };

            _context.Albums.Add(newAlbum);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}

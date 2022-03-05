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

namespace _2021p3web_gallery_PatrikMrnka.Pages.Albums.Specificc
{
    public class IndexModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public IndexModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public string NotFoundAlbum { get; set; }
        public int PicturesInAlbum { get; set; }
        public Guid? AlbumIdReceived { get; set; }
        public Guid? AlbumIdDefault { get; set; }
        public Album Album { get; set; }
        public IList<Album> Albums { get;set; }
        public StoredFile Picture { get; set; }
        public IList<StoredFile> Pictures { get; set; }
        public StoredFileAlbum PictureAlbum { get; set; }
        [BindProperty]
        public IList<StoredFileAlbum> PicturesAlbum { get; set; }


        public async Task OnGetAsync(Guid? albumid)
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdReceived = albumid; // oveření do PageModel, jestli bylo nějaké AlbumId obdrženo - pokud ne, nezobrazí se nic

            if (albumid == null)
            {
                NotFoundAlbum = "Nebylo zvoleno konkrétní album.";
            }
            else if (albumid != null)
            {
                Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                AlbumIdReceived = albumid; // oveření do PageModel, jestli bylo nějaké AlbumId obdrženo - pokud ne, nezobrazí se nic
                AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba
                PicturesInAlbum = _context.StoredFileAlbums.Include(a => a.Album).Where(a => a.AlbumId == albumid).Count(); // počet fotek v albu
                PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Album).Include(b => b.Picture).Where(a => a.AlbumId == albumid).ToListAsync(); // fotky v konkrétním albu
            }
        }

        public async Task<IActionResult> OnPostDeleteAlbum(Guid? albumid) // Tlačítko smazat album
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
            if (albumid == null && albumid != AlbumIdDefault)
            {
                return Page();
            }
            else if (albumid != null && albumid == AlbumIdDefault)
            {
                return Page();
            }
            else if (albumid != null && albumid != AlbumIdDefault)
            {
                // smazat album
                Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                _context.Albums.Remove(Album);
                await _context.SaveChangesAsync();
                return RedirectToPage("../Index");
            }
            return Page();
        }
    }
}

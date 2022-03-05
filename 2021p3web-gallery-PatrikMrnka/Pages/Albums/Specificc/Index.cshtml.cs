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
        private IWebHostEnvironment _environment;

        public IndexModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        public string NotFoundAlbum { get; set; }
        public int PicturesInAlbum { get; set; }
        public bool IsAlbumPublic { get; set; }
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
                IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();
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

        public async Task<IActionResult> OnPostMakeAlbumPrivate(Guid? albumid) // Tlačítko nastavit jako soukromé
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
            IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();

            if (albumid == null && albumid != AlbumIdDefault)
            {
                return RedirectToPage(new {albumid = albumid});
            }
            else if (albumid != null && albumid == AlbumIdDefault)
            {
                return RedirectToPage(new { albumid = albumid });
            }
            else if (albumid != null && albumid != AlbumIdDefault && IsAlbumPublic == true)
            {
                Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                Album.IsPublic = false;
                await _context.SaveChangesAsync();

                PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Album).Where(a => a.AlbumId == albumid).ToListAsync(); // list všech záznamů obrázku z určitého alba
                foreach (var item in PicturesAlbum)
                {
                    Picture = await _context.Files.Include(a => a.Uploader).Include(b => b.StoredFileAlbums).Where(a => a.UploaderId == userId).Where(b => b.Id == item.PictureId).FirstOrDefaultAsync();
                    Picture.IsPublic = false;
                    await _context.SaveChangesAsync();
                }
                return RedirectToPage(new { albumid = albumid });
            }
            return RedirectToPage(new { albumid = albumid });
        }

        public async Task<IActionResult> OnPostMakeAlbumPublic(Guid? albumid) // Tlačítko nastavit jako veřejné
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
            IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();

            if (albumid == null && albumid != AlbumIdDefault)
            {
                return RedirectToPage(new { albumid = albumid });
            }
            else if (albumid != null && albumid == AlbumIdDefault)
            {
                return RedirectToPage(new { albumid = albumid });
            }
            else if (albumid != null && albumid != AlbumIdDefault && IsAlbumPublic == false)
            {
                Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                Album.IsPublic = true;
                await _context.SaveChangesAsync();

                PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Album).Where(a => a.AlbumId == albumid).ToListAsync(); // list všech záznamů obrázku z určitého alba
                foreach (var item in PicturesAlbum) // pro každý záznam 
                {
                    Picture = await _context.Files.Include(a => a.Uploader).Include(b => b.StoredFileAlbums).Where(a => a.UploaderId == userId).Where(b => b.Id == item.PictureId).FirstOrDefaultAsync();
                    Picture.IsPublic = true;
                    await _context.SaveChangesAsync();
                }
                return RedirectToPage(new { albumid = albumid });

            }
            return RedirectToPage(new { albumid = albumid });
        }

        public async Task<IActionResult> OnPostDeletePhoto(Guid? albumid, Guid? pictureid) // odstranit fotku kompletně
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
            IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();

            if (pictureid == null && albumid == null)
            {
                return RedirectToPage(new { albumid = albumid });
            }
            else if (pictureid != null && albumid == null)
            {
                return RedirectToPage(new { albumid = albumid });
            }
            else if (pictureid == null && albumid != null)
            {
                return RedirectToPage(new { albumid = albumid });
            }
            else if (pictureid != null && albumid != null)
            {
                var pictures = _context.StoredFileAlbums.Include(a => a.Picture).Where(a => a.PictureId == pictureid);
                foreach (var item in await pictures.ToListAsync()) // vymazat fotku ze všech alb
                {
                    _context.StoredFileAlbums.Remove(item);
                }
                await _context.SaveChangesAsync();

                Picture = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.Id == pictureid).FirstOrDefaultAsync(); // vymazat fotku z Files
                _context.Files.Remove(Picture);
                await _context.SaveChangesAsync();

                var thumbnails = _context.Thumbnails.Include(a => a.File).Where(a => a.FileId == pictureid);
                foreach (var item in await thumbnails.ToListAsync()) // vymazat náhledy fotky
                {
                    _context.Thumbnails.Remove(item);
                }
                await _context.SaveChangesAsync();

                //vymazat fotku na disku
                var pictureIdString = Path.Combine(_environment.ContentRootPath, "Uploads", pictureid.ToString()); // cesta k fotce na disku
                if (System.IO.File.Exists(pictureIdString)) // pokud záznam o fotce na disku existuje
                {
                    System.IO.File.Delete(pictureIdString);
                }

                return RedirectToPage(new { albumid = albumid });
            }
            return RedirectToPage(new { albumid = albumid });
        }

        public async Task<IActionResult> OnPostDeletePhotoAlbum(Guid? albumid, Guid? pictureid) // odstranit fotku z konkrétního alba
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
            IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();

            if (pictureid == null && albumid == null && albumid != AlbumIdDefault)
            {
                return RedirectToPage(new { albumid = albumid });
            }
            else if (pictureid != null && albumid == null && albumid != AlbumIdDefault)
            {
            return RedirectToPage(new { albumid = albumid });
            }
            else if (pictureid == null && albumid != null && albumid != AlbumIdDefault)
            {
                return RedirectToPage(new { albumid = albumid });
            }
            else if (pictureid != null && albumid == AlbumIdDefault)
            {
                return RedirectToPage(new { albumid = albumid });
            }
            else if (pictureid != null && albumid != null && albumid != AlbumIdDefault)
            { 
                PictureAlbum = await _context.StoredFileAlbums.Include(a => a.Picture).Include(b => b.Album).Where(a => a.PictureId == pictureid).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                _context.StoredFileAlbums.Remove(PictureAlbum);
                await _context.SaveChangesAsync();
                return RedirectToPage(new { albumid = albumid });
            }
            return RedirectToPage(new { albumid = albumid });
        }
    }
}

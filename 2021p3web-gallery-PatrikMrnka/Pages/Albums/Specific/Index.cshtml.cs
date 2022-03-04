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

namespace _2021p3web_gallery_PatrikMrnka.Pages.Albums.Specific
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public IndexModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private IWebHostEnvironment _environment;
        public string? AlbumName { get; set; }
        public int PictureInAlbumCount { get; set; }
        public string ErrorMessage { get; set; }
        public string DeleteMessage { get; set; }
        public Guid? AlbumId { get; set; }
        public Guid? AlbumIdDefault { get; set; }
        public IList<StoredFileAlbum> PictureId { get; set; }
        public IList<StoredFileAlbum> Pictures { get; set; }
        [BindProperty]
        public StoredFileAlbum StoredFileAlbum { get; set; }
        [BindProperty]
        public IList<StoredFileAlbum> StoredFileAlbums { get; set; }
        [BindProperty]
        public Album Album { get; set; }
        [BindProperty]
        public StoredFile StoredFile { get; set; }
        [BindProperty]
        public Thumbnail Thumbnail { get; set; }
        public async Task OnGetAsync(Guid? id)
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumId = id;
            AlbumIdDefault = _context.Albums.Where(a => a.UploaderId == userId).Where(a => a.IsDefault == true).Select(a => a.AlbumId).FirstOrDefault();


            if (id == null)
            {
                AlbumName = "Neidentifikováno";
                PictureInAlbumCount = 0;
                ErrorMessage = "Není odkazováno na určité album. Vyberte na stránce alba určité album";
            }
            else if (id != null)
            {
                AlbumName = await _context.Albums.Where(a => a.AlbumId == id).Select(b => b.Name).FirstOrDefaultAsync();
                PictureInAlbumCount = _context.StoredFileAlbums.Include(a => a.Picture).Where(b => b.AlbumId == id).Count();
                Pictures = await _context.StoredFileAlbums.Include(a => a.Picture).Where(b => b.AlbumId == id).ToListAsync();
            }
        }

        public async Task<IActionResult> OnGetDeleteAlbum(Guid? albumid)
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Where(a => a.UploaderId == userId).Where(a => a.IsDefault == true).Select(a => a.AlbumId).FirstOrDefault();
            if (albumid == null)
            {
                return Page();
            }
            else if(albumid != null && albumid != AlbumIdDefault)
            {
                Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                _context.Albums.Remove(Album);
                await _context.SaveChangesAsync();
                return RedirectToPage("../Index");
            }
            else if(albumid == AlbumIdDefault)
            {
                return RedirectToPage("../Index");
            }
            return RedirectToPage("../Index");
        }

        public async Task<IActionResult> OnGetDeletePhoto(Guid? pictureid, Guid? albumid) // vymazat fotku kompletně
        {
            if (pictureid == null && albumid == null) 
            {
                return RedirectToPage(albumid);
            }
            else if(pictureid != null && albumid == null)
            {
                return RedirectToPage(albumid);
            }
            else if(pictureid == null && albumid != null)
            {
                return RedirectToPage(albumid);
            }
            else if(pictureid != null && albumid != null)
            {
                var pictures = _context.StoredFileAlbums.Include(a => a.Picture).Where(a => a.PictureId == pictureid);
                foreach (var item in await pictures.ToListAsync()) // vymazat fotku ze všech alb
                {
                    _context.StoredFileAlbums.Remove(item);
                }
                await _context.SaveChangesAsync();
                //StoredFileAlbums = await _context.StoredFileAlbums
                //    .Include(a => a.Picture).Include(b => b.Album).Where(a => a.PictureId == pictureid).Where(b => b.AlbumId == albumid).ToListAsync(); // vymazat fotku z alba
                //_context.StoredFileAlbums.Remove(StoredFileAlbums);

                var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného 
                StoredFile = await _context.Files
                    .Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.Id == pictureid).FirstOrDefaultAsync(); // vymazat fotku z Files
                _context.Files.Remove(StoredFile);
                await _context.SaveChangesAsync();

                var thumbnails = _context.Thumbnails.Include(a => a.File).Where(a => a.FileId == pictureid);
                foreach (var item in await thumbnails.ToListAsync()) // vymazat náhledy fotky
                {
                    _context.Thumbnails.Remove(Thumbnail);
                }
                await _context.SaveChangesAsync();

                //vymazat fotku na disku
                var pictureIdString = Path.Combine(_environment.ContentRootPath, "Uploads", pictureid.ToString()); // cesta k fotce na disku
                if(System.IO.File.Exists(pictureIdString)) // pokud záznam o fotce na disku existuje
                {
                    System.IO.File.Delete(pictureIdString);
                }

                return RedirectToPage(albumid);
            }
            return RedirectToPage(albumid);
        }

        public async Task<IActionResult> OnGetDeletePhotoAlbum(Guid? pictureid, Guid? albumid) // vymazat fotku z konkrétního alba
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného 
            AlbumIdDefault = _context.Albums.Where(a => a.UploaderId == userId).Where(a => a.IsDefault == true).Select(a => a.AlbumId).FirstOrDefault();
            if (pictureid == null && albumid == null)
            {
                return RedirectToPage(albumid);
            }
            else if(pictureid != null && albumid == null)
            {
                return RedirectToPage(albumid);
            }
            else if(pictureid == null && albumid != null)
            {
                return RedirectToPage(albumid);
            }
            else if(pictureid != null && albumid == AlbumIdDefault)
            {
                return RedirectToPage(albumid);
            }
            else if(pictureid != null && albumid != null)
            {
                StoredFileAlbum = await _context.StoredFileAlbums
                    .Include(a => a.Picture).Include(b => b.Album).Where(a => a.PictureId == pictureid).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                _context.StoredFileAlbums.Remove(StoredFileAlbum);
                await _context.SaveChangesAsync();
                return RedirectToPage(albumid);
            }
            return RedirectToPage(albumid);
        }
       
    }
}

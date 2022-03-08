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
    public class PictureModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;
        private IWebHostEnvironment _environment;


        public PictureModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public string PictureName { get; set; }
        public DateTime PictureDate { get; set; }
        public Guid? PictureOwnerId { get; set; }
        public bool PicturePublic { get; set; }
        public string ErrorMessage { get; set; }
        public string UserId { get; set; }
        public string NotFoundPicture { get; set; }
        public int PicturesInAlbum { get; set; }
        public int PublicPicturesCount { get; set; }
        public bool IsAlbumPublic { get; set; }
        public Guid? PictureIdReceived { get; set; }
        public Guid? AlbumIdDefault { get; set; }
        public Guid? AlbumIdReceived { get; set; }
        public string AlbumOwnerId { get; set; }
        public Album Album { get; set; }
        public IList<Album> Albums { get; set; }
        public StoredFile Picture { get; set; }
        public IList<StoredFile> Pictures { get; set; }
        public StoredFileAlbum PictureAlbum { get; set; }
        public bool IsPictureInAlbum { get; set; }
        [BindProperty]
        public IList<StoredFileAlbum> PicturesAlbum { get; set; }
        public List<StoredFileListViewModel> Files { get; set; } = new();
        public Thumbnail Thumbnail { get; set; }


        public async Task OnGetAsync(Guid? pictureid, Guid? albumid)
        {
            if (pictureid == null && albumid == null) // pokud není odkazováno na žádnou fotku ani album
            {
                Files = _context.Files
                    .AsNoTracking()
                    .Include(f => f.Uploader)
                    .Include(f => f.Thumbnails)
                    .Where(a => a.IsPublic == true)
                    .Select(f => new StoredFileListViewModel
                    {
                        Id = f.Id,
                        ContentType = f.ContentType,
                        OriginalName = f.OriginalName,
                        UploaderId = f.UploaderId,
                        Uploader = f.Uploader,
                        UploadedAt = f.UploadedAt,
                        ThumbnailCount = f.Thumbnails.Count,
                        IsPublic = f.IsPublic
                    }).ToList();
            }
            else if (pictureid != null && albumid == null)
            {
                AlbumIdReceived = albumid;
                PictureIdReceived = pictureid;
                var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
                Picture = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(a => a.Id == pictureid).FirstOrDefaultAsync();
                if (Picture.UploaderId == userId || Picture.IsPublic == true) // pokud jsem vlastník fotky či je veřejná
                {
                    Picture = await _context.Files.Where(a => a.Id == pictureid).FirstOrDefaultAsync();
                    AlbumIdReceived = albumid;
                    PictureIdReceived = pictureid;
                }
                else
                {
                    ErrorMessage = "Nejsi vlastník fotky nebo není veřejná.";
                }
            }
            else if (pictureid != null && albumid != null)
            {
                var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
                Picture = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.Id == pictureid).FirstOrDefaultAsync();
                Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                AlbumIdReceived = albumid;
                PictureIdReceived = pictureid;
                if (Picture.UploaderId == userId || Picture.IsPublic == true) // pokud jsem vlastník fotky či je veřejná
                {
                    if (Album.UploaderId == userId || Album.IsPublic == true) // pokud jsem vlastník alba či je veřejné
                    {
                        Picture = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.Id == pictureid).FirstOrDefaultAsync();
                        Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                        AlbumIdReceived = albumid;
                        PictureIdReceived = pictureid;
                    }
                    else // pokud nejsem vlastník alba ani není veřejné
                    {
                        ErrorMessage = "Nejsi vlastník alba nebo není veřejné.";
                    }
                }
                else // pokud nejsem vlastník fotky ani není veřejná
                {
                    ErrorMessage = "Nejsi vlastník fotky nebo není veřejná.";
                }
            }
            //PictureIdReceived = pictureid;
            //PublicPicturesCount = _context.Files.Where(a => a.IsPublic == true).Count();
            //if (pictureid == null && albumid == null)
            //{

            //}
            //else if (pictureid != null && albumid == null)
            //{
            //    //var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            //    Picture = await _context.Files.Where(a => a.Id == pictureid).FirstOrDefaultAsync();
            //    PictureName = await _context.Files.Where(a => a.Id == pictureid).Select(a => a.OriginalName).FirstOrDefaultAsync();
            //    PictureDate = await _context.Files.Where(a => a.Id == pictureid).Select(a => a.UploadedAt).FirstOrDefaultAsync();
            //    PicturePublic = await _context.Files.Where(b => b.Id == pictureid).Select(a => a.IsPublic).FirstOrDefaultAsync();
            //    PictureOwnerId = await _context.Files.Include(a => a.Uploader).Where(b => b.Id == pictureid).Select(a => a.UploaderId).FirstOrDefaultAsync();

            //}
            //else if (pictureid != null && albumid != null)
            //{
            //    UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            //    Picture = await _context.Files.Where(a => a.Id == pictureid).FirstOrDefaultAsync();
            //    PictureName = await _context.Files.Where(a => a.Id == pictureid).Select(a => a.OriginalName).FirstOrDefaultAsync();
            //    PictureDate = await _context.Files.Where(a => a.Id == pictureid).Select(a => a.UploadedAt).FirstOrDefaultAsync();
            //    PictureOwnerId = await _context.Files.Include(a => a.Uploader).Where(b => b.Id == pictureid).Select(a => a.UploaderId).FirstOrDefaultAsync();
            //    PicturePublic = await _context.Files.Where(b => b.Id == pictureid).Select(a => a.IsPublic).FirstOrDefaultAsync();
            //    AlbumOwnerId = await _context.Albums.Include(a => a.Uploader).Where(b => b.AlbumId == albumid).Select(a => a.UploaderId).FirstOrDefaultAsync();
            //    IsPictureInAlbum = _context.StoredFileAlbums.Include(a => a.Picture).Include(b => b.Album).Where(a => a.PictureId == pictureid).Where(b => b.AlbumId == albumid).Any();// soubor v galerii
            //    AlbumIdReceived = albumid;
            //    PictureIdReceived = pictureid;
            //}
        }

        public async Task<IActionResult> OnGetThumbnail(string filename)
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            Picture = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.Id == pictureid).FirstOrDefaultAsync();
            if (Picture.UploaderId == userId || Picture.IsPublic == true) // pokud jsem vlastník fotky nebo je veřejná
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
                  .Where(t => t.FileId == Guid.Parse(filename))
                  .SingleOrDefaultAsync();
                if (thumbnail != null)
                {
                    return File(thumbnail.Blob, file.ContentType);
                }
                return NotFound("no thumbnail for this file");
            }
            else // pokud nejsem vlastník fotky a ani není veřejná
            {
                return RedirectToPage("./Index");
            }
        }

        public IActionResult OnGetFullSize(string filename) // fotka v plné velikosti/kvalitě
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            Picture = _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.Id == pictureid).FirstOrDefaul();
            if (Picture.UploaderId == userId || Picture.IsPublic == true) // pokud jsem vlastník fotky nebo je veřejná
            {
                var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", filename);

                if (System.IO.File.Exists(fullName))
                {
                    var fileRecord = _context.Files.Find(Guid.Parse(filename));

                    if (fileRecord != null)
                    {
                        return File(System.IO.File.OpenRead(fullName), fileRecord.ContentType);
                    }
                    else
                    {
                        return RedirectToPage("./Index");
                    }
                }
                else
                {
                    return RedirectToPage("./Index");
                }
            }
            return RedirectToPage("./Index");

        }

        public async Task<IActionResult> OnPostDeletePhoto(Guid? albumid, Guid? pictureid) // odstranit fotku kompletně
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
            IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();
            Picture = _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.Id == pictureid).FirstOrDefault();

            if (pictureid == null && albumid == null)
            {
                return RedirectToPage(new { albumid = albumid });
            }
            else if (pictureid != null && albumid == null) // pokud je odkázáno pouze na fotku bez alba
            {
                if(Picture.UploaderId == userId) // pokud jsem vlastník fotky
                {
                    PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Picture).Where(a => a.PictureId == pictureid).ToListAsync(); // záznamy o dané fotce
                    foreach (var item in PicturesAlbum) // vymazat fotku ze všech alb
                    {
                        _context.StoredFileAlbums.Remove(item);
                    }
                    await _context.SaveChangesAsync();

                    Picture = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.Id == pictureid).FirstOrDefaultAsync(); // vymazat fotku z Files
                    _context.Files.Remove(Picture);
                    await _context.SaveChangesAsync();

                    Thumbnail = await _context.Thumbnails.Include(a => a.File).Where(a => a.FileId = pictureid); // vymazat náhled fotky
                    _context.Thumbnails.Remove(Thumbnail);
                    await _context.SaveChangesAsync();

                    //vymazat fotku na disku
                    var pictureIdString = Path.Combine(_environment.ContentRootPath, "Uploads", pictureid.ToString()); // cesta k fotce na disku
                    if (System.IO.File.Exists(pictureIdString)) // pokud záznam o fotce na disku existuje
                    {
                        System.IO.File.Delete(pictureIdString);
                    }
                    return RedirectToPage("./Index");
                }
                else // pokud nejsem vlastník fotky
                {
                    return RedirectToPage(new { pictureid = pictureid });
                }
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

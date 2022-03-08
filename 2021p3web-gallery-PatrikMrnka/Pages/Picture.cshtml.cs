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

namespace _2021p3web_gallery_PatrikMrnka.Pages.Albums.Specificc
{
    [Authorize]
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
                PictureIdReceived = pictureid;
                PublicPicturesCount = _context.Files.Where(b => b.IsPublic == true).Count();
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
                if (User.Identity.IsAuthenticated)
                {
                    Picture = await _context.Files.Where(a => a.Id == pictureid).FirstOrDefaultAsync();
                    UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
                    Picture = await _context.Files.Where(a => a.Id == pictureid).FirstOrDefaultAsync();
                    if (Picture.UploaderId == UserId || Picture.IsPublic == true) // pokud jsem vlastník fotky či je veřejná
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
                else
                {
                }
                
            }
            else if (pictureid != null && albumid != null)
            {
                AlbumIdReceived = albumid;
                PictureIdReceived = pictureid;
                UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
                Picture = await _context.Files.Include(a => a.Uploader).Where(b => b.Id == pictureid).FirstOrDefaultAsync();
                Album = await _context.Albums.Include(a => a.Uploader).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                if (Album.UploaderId == UserId) // pokud jsem vlastník alba
                {
                    if (Album.IsPublic == true)
                    {
                        if (Picture.UploaderId == UserId) // pokud jsem vlastník fotky
                        {
                            Picture = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.Id == pictureid).FirstOrDefaultAsync();
                            Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
                            AlbumIdReceived = albumid;
                            PictureIdReceived = pictureid;
                        }
                        else if (Picture.IsPublic == true && Picture.UploaderId != UserId) // pokud je fotka veřejná, ale nevlastním ji
                        {
                            Picture = await _context.Files.Where(b => b.Id == pictureid).FirstOrDefaultAsync();
                            Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
                            AlbumIdReceived = albumid;
                            PictureIdReceived = pictureid;
                        }
                    }
                    else if (Album.IsPublic != true)
                    {
                        if (Picture.UploaderId == UserId) // pokud jsem vlastník fotky
                        {
                            Picture = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.Id == pictureid).FirstOrDefaultAsync();
                            Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat

                            AlbumIdReceived = albumid;
                            PictureIdReceived = pictureid;
                        }
                        else if (Picture.IsPublic == true && Picture.UploaderId != UserId) // pokud je fotka veřejná, ale nevlastním ji
                        {
                            Picture = await _context.Files.Where(b => b.Id == pictureid).FirstOrDefaultAsync();
                            Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
                            AlbumIdReceived = albumid;
                            PictureIdReceived = pictureid;
                        }
                    }
                }
                else if (Album.UploaderId != UserId) // Pokud nejsem vlastník alba
                {
                    if (Album.IsPublic == true)
                    {
                        if (Picture.UploaderId == UserId) // Pokud jsem vlastník fotky
                        {
                            Picture = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.Id == pictureid).FirstOrDefaultAsync();
                            Album = await _context.Albums.Where(a => a.AlbumId == albumid).FirstOrDefaultAsync();
                            AlbumIdReceived = albumid;
                            PictureIdReceived = pictureid;
                        }
                        else if (Picture.IsPublic == true && Picture.UploaderId != UserId) // pokud je fotka veřejná, ale nevlastním ji
                        {
                            Picture = await _context.Files.Where(b => b.Id == pictureid).FirstOrDefaultAsync();
                            Album = await _context.Albums.Where(a => a.AlbumId == albumid).FirstOrDefaultAsync();
                            AlbumIdReceived = albumid;
                            PictureIdReceived = pictureid;
                        }
                    }
                    else if (Album.IsPublic == false)
                    {
                        ErrorMessage = "Nelze Zobrazit.";
                    }
                }
                
            }
        }

        public async Task<IActionResult> OnGetThumbnail(string filename, Guid? pictureid)
        {
            PictureIdReceived = pictureid;
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            Picture = await _context.Files.Where(b => b.Id == PictureIdReceived).FirstOrDefaultAsync();
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

        public IActionResult OnGetFullSize(string filename, Guid? pictureid) // fotka v plné velikosti/kvalitě
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            Picture = _context.Files.Where(b => b.Id == pictureid).FirstOrDefault();
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

        public async Task<IActionResult> OnPostDeletePhoto(Guid? pictureid) // odstranit fotku kompletně
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
 
            Picture = _context.Files.Include(a => a.Uploader).Where(b => b.Id == pictureid).FirstOrDefault();

            if (pictureid == null)
            {
                return RedirectToPage(new { pictureid=pictureid});
            }
            else if (pictureid != null) // pokud je odkázáno pouze na fotku
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

                    //Thumbnail = await _context.Thumbnails.Include(a => a.File).Where(a => a.FileId == pictureid).FirstOrDefaultAsync(); // vymazat náhled fotky
                    //var thumbnail = await _context.Thumbnails.Include(a => a.File).Where(a => a.FileId == pictureid).FirstOrDefaultAsync();
                    //_context.Thumbnails.Remove(thumbnail);
                    //await _context.SaveChangesAsync();

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
                    if (Picture.IsPublic == true) // Pokud je fotka veřejná
                    {
                        PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Picture).Where(a => a.PictureId == pictureid).ToListAsync(); // záznamy o dané fotce
                        AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
                        var userAlbums = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).ToListAsync();

                        PictureAlbum = await _context.StoredFileAlbums.Include(a => a.Picture).Include(b => b.Album).Where(a => a.PictureId == pictureid).Where(b => b.AlbumId == AlbumIdDefault).FirstOrDefaultAsync();
                        _context.StoredFileAlbums.Remove(PictureAlbum);
                        await _context.SaveChangesAsync();

                    }
                    return RedirectToPage("./Albums/Index");
                }
                return RedirectToPage(new { pictureid=Picture });
            }
            return RedirectToPage(new { pictureid = pictureid });
        }

        public async Task<IActionResult> OnPostDeletePhotoAlbum(Guid? albumid, Guid? pictureid) // odstranit fotku z konkrétního alba
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
            IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();
            var albumIdStranger = await _context.Albums.Include(a => a.Uploader).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();

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
                Picture = await _context.Files.Where(a => a.Id == pictureid).FirstOrDefaultAsync();
                Album = await _context.Albums.Where(a => a.AlbumId == albumid).FirstOrDefaultAsync();
                if (Picture.UploaderId == userId) // pokud vlastním fotku
                {
                    if (Album.UploaderId == userId) // pokud vlastním album
                    {
                        PictureAlbum = await _context.StoredFileAlbums.Include(a => a.Picture).Include(b => b.Album).Where(a => a.PictureId == pictureid).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                        _context.StoredFileAlbums.Remove(PictureAlbum);
                        await _context.SaveChangesAsync();

                    }
                    else if (Album.UploaderId != userId) // pokud nevlastním album
                    {

                    }
                }
                else if (Picture.UploaderId != userId) // pokud nevlastním fotku
                {
                    if (Picture.IsPublic == true) // pokud je fotka veřejná
                    {
                        Picture = await _context.Files.Where(a => a.Id == pictureid).FirstOrDefaultAsync();
                        Album = await _context.Albums.Where(a => a.AlbumId == albumid).FirstOrDefaultAsync();

                        PictureAlbum = await _context.StoredFileAlbums.Include(a => a.Picture).Include(b => b.Album).Where(a => a.PictureId == pictureid).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                        _context.StoredFileAlbums.Remove(PictureAlbum);
                        await _context.SaveChangesAsync();
                        return RedirectToPage("./Albums/Specificc/Index", new { albumid = albumid });
                    }

                }
                

            }
            return RedirectToPage(new { pictureid = pictureid, albumid = albumid });
        }

        public async Task<IActionResult> OnPostMakePhotoPublic(Guid? pictureid, Guid? albumid) // zveřejnit fotku
        {
            UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            Picture = await _context.Files.Include(a => a.Uploader).Where(b => b.Id == pictureid).FirstOrDefaultAsync();
            if (pictureid != null)
            {
                if (Picture.UploaderId == UserId) // pokud jsem vlastník fotky
                {
                    if (Picture.IsPublic == false) // pokud je fotka soukromá
                    {
                        Picture.IsPublic = true;
                        await _context.SaveChangesAsync();
                        return RedirectToPage(new { pictureid = pictureid, albumid = albumid });
                    }
                    else
                    {
                        return RedirectToPage(new { pictureid = pictureid, albumid = albumid });
                    }
                }
                else
                {
                    return RedirectToPage(new { pictureid = pictureid, albumid = albumid });
                }
            }
            else
            {
                return RedirectToPage(new { pictureid = pictureid, albumid = albumid });
            }
        }

        public async Task<IActionResult> OnPostMakePhotoPrivate(Guid? pictureid, Guid? albumid)
        {
            UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            Picture = await _context.Files.Include(a => a.Uploader).Where(b => b.Id == pictureid).FirstOrDefaultAsync();
            if (pictureid != null)
            {
                if (Picture.UploaderId == UserId) // pokud jsem vlastník fotky
                {
                    if (Picture.IsPublic == true)
                    {
                        Picture.IsPublic = false;
                        await _context.SaveChangesAsync();
                        return RedirectToPage(new { pictureid = pictureid, albumid = albumid });
                    }
                    else
                    {
                        return RedirectToPage(new { pictureid = pictureid, albumid = albumid });
                    }
                }
                else
                {
                    return RedirectToPage(new { pictureid = pictureid, albumid = albumid });
                }
            }
            else
            {
                return RedirectToPage(new { pictureid = pictureid, albumid = albumid });
            }
        }

    }
}

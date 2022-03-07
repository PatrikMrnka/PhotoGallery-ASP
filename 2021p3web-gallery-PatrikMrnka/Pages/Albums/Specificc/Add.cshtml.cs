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
    public class AddModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public AddModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }



        public string NotFoundAlbum { get; set; }
        public string PictureName { get; set; }
        public bool PictureExist { get; set; }
        public string PictureOwner { get; set; }
        public bool PicturePublic { get; set; }
        public string NotFoundPicture { get; set; }
        public string ErrorMessage { get; set; }
        public bool AlbumExist { get; set; }
        public int PicturesInAlbum { get; set; }
        public bool IsAlbumPublic { get; set; }
        public Guid? PictureIdReceived { get; set; }
        public Guid? AlbumIdDefault { get; set; }
        public string AlbumOwner { get; set; }
        public string AlbumName { get; set; }
        public string UserId { get; set; }
        public bool PictureAlbumRecord { get; set; }
        public bool PictureAlbumRecordDefault { get; set; }
        public Album Album { get; set; }
        public Album AlbumDefault { get; set; }
        public IList<Album> Albums { get; set; }
        public StoredFile Picture { get; set; }
        public IList<StoredFile> Pictures { get; set; }
        public StoredFileAlbum PictureAlbum { get; set; }
        [BindProperty]
        public IList<StoredFileAlbum> PicturesAlbum { get; set; }

        public async Task OnGetAsync(Guid? pictureid) // Př
        {
            PictureExist = _context.Files.Where(a => a.Id == pictureid).Any();
            Albums = await _context.Albums.Include(b => b.Uploader).Where(b => b.UploaderId == UserId).ToListAsync();
            AlbumDefault = await _context.Albums.Include(b => b.Uploader).Where(b => b.UploaderId == UserId).Where(a => a.IsDefault == true).FirstOrDefaultAsync();
            if (pictureid != null)
            {
                UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
                PictureOwner = await _context.Files.Include(b => b.Uploader).Where(a => a.Id == pictureid).Select(b => b.UploaderId).FirstOrDefaultAsync();
                if (PictureOwner == UserId || PicturePublic == true) // pokud jsem vlastník fotky nebo je veřejná
                {
                    PictureIdReceived = pictureid;
                    PictureName = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == UserId).Where(b => b.Id == pictureid).Select(b => b.OriginalName).FirstOrDefaultAsync();
                    Albums = await _context.Albums.Include(b => b.Uploader).Where(b => b.UploaderId == UserId).ToListAsync();
                }
                else // pokud nejsem vlastník fotky nebo není veřejná
                {
                    ErrorMessage = "Nejsi vlastníkem fotky nebo není veřejná.";
                }
            }
            else
            {
                ErrorMessage = "Není odkázáno na konkrétní fotku.";
            }
        }

        public async Task<IActionResult> OnPostAdd(Guid? pictureid, Guid? albumid) // tlačítko přidat do alba
        {
            if (pictureid != null && albumid != null) // pokud je zadáno obojí
            {
                UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
                PictureOwner = await _context.Files.Include(b => b.Uploader).Where(a => a.Id == pictureid).Select(b => b.UploaderId).FirstOrDefaultAsync(); // Vlastník fotky
                AlbumOwner = await _context.Albums.Include(b => b.Uploader).Where(a => a.AlbumId == albumid).Select(b => b.UploaderId).FirstOrDefaultAsync(); // Vlastník alba
                PicturePublic = await _context.Files.Where(a => a.Id == pictureid).Select(b => b.IsPublic).FirstOrDefaultAsync();
                AlbumDefault = await _context.Albums.Include(b => b.Uploader).Where(b => b.UploaderId == UserId).Where(a => a.IsDefault == true).FirstOrDefaultAsync();
                PictureAlbumRecord = await _context.StoredFileAlbums.Where(a => a.PictureId == pictureid).Where(b => b.AlbumId == albumid).AnyAsync(); // je záznam v albu
                PictureAlbumRecordDefault = await _context.StoredFileAlbums.Where(a => a.PictureId == pictureid).Where(b => b.AlbumId == AlbumDefault.AlbumId).AnyAsync(); // je záznam ve výchozím albu
                ErrorMessage = "Nelze přidat.";
                if (PictureOwner == UserId || PicturePublic == true) // pokud vlastním fotku nebo je fotka veřejná
                {
                    if (AlbumOwner == UserId) // pokud jsem vlastník alba
                    {
                        if (PictureAlbumRecord != true) // pokud ještě záznam v tabulce není
                        {
                            if (PictureAlbumRecordDefault != true) // pokud ještě není záznam v tabulce Výchozí
                            {
                                // přidat nový záznam do PictureAlbum (výchozí + album)
                                AlbumDefault = await _context.Albums.Include(b => b.Uploader).Where(b => b.UploaderId == UserId).Where(a => a.IsDefault == true).FirstOrDefaultAsync(); // výchozí album
                                var albumPictureRecordDefault = new StoredFileAlbum // výchozí
                                {
                                    AlbumId = AlbumDefault.AlbumId,
                                    PictureId = pictureid
                                };

                                _context.StoredFileAlbums.Add(albumPictureRecordDefault);
                                await _context.SaveChangesAsync();

                                var albumPictureRecord = new StoredFileAlbum
                                {
                                    AlbumId = albumid,
                                    PictureId = pictureid
                                };

                                _context.StoredFileAlbums.Add(albumPictureRecord);
                                await _context.SaveChangesAsync();
                                return RedirectToPage("./Index", new { albumid = albumid });
                            }
                            else // pouze do zvoleného alba
                            {
                                var albumPictureRecord = new StoredFileAlbum
                                {
                                    AlbumId = albumid,
                                    PictureId = pictureid
                                };

                                _context.StoredFileAlbums.Add(albumPictureRecord);
                                await _context.SaveChangesAsync();
                                return RedirectToPage("./Index", new { albumid = albumid });
                            }

                        }
                        else // pokud záznam v tabulce je
                        {
                            ErrorMessage = "Nelze přidat. Fotka už je v albu.";
                            return RedirectToPage(new { pictureid = pictureid});
                        }
                    }
                    else // pokud nejsem vlastník alba
                    {
                        ErrorMessage = "Nelze přidat. Nejsi vlastník alba.";
                        return RedirectToPage(new {pictureid = pictureid});
                    }
                }
                else // pokud fotku nevlastním nebo není veřejná
                {
                    ErrorMessage = "Nelze přidat. Nejsi vlastníkem fotky nebo není veřejná.";
                }
            }
            else
            {
                ErrorMessage = "Není zvolena určitá fotka či album.";
                return RedirectToPage(new { pictureid = pictureid, albumid = albumid });
            }
            return RedirectToPage(new { pictureid = pictureid, albumid = albumid });
        }
    }
}

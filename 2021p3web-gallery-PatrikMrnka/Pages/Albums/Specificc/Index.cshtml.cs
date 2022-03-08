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
        [BindProperty]
        public string ChangeName { get; set; }
        public string UserId { get; set; }
        public Guid? AlbumIdReceived { get; set; }
        public Guid? AlbumIdDefault { get; set; }
        public Album Album { get; set; }
        public IList<Album> Albums { get;set; }
        public StoredFile Picture { get; set; }
        public IList<StoredFile> Pictures { get; set; }
        public StoredFileAlbum PictureAlbum { get; set; }
        [BindProperty]
        public IList<StoredFileAlbum> PicturesAlbum { get; set; }


        public async Task OnGetAsync(Guid? albumid, int? filterid)
        {

            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdReceived = albumid; // oveření do PageModel, jestli bylo nějaké AlbumId obdrženo - pokud ne, nezobrazí se nic

            if (albumid == null)
            {
                NotFoundAlbum = "Nebylo zvoleno konkrétní album.";
            }
            else if (albumid != null && filterid == null)
            {
                UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
                Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                AlbumIdReceived = albumid; // oveření do PageModel, jestli bylo nějaké AlbumId obdrženo - pokud ne, nezobrazí se nic
                AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba
                PicturesInAlbum = _context.StoredFileAlbums.Include(a => a.Album).Where(a => a.AlbumId == albumid).Count(); // počet fotek v albu
                PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Album).Include(b => b.Picture).Where(a => a.AlbumId == albumid).ToListAsync(); // fotky v konkrétním albu
                IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();
            }
            else if (albumid != null && filterid == 1)
            {
                UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
                Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                AlbumIdReceived = albumid; // oveření do PageModel, jestli bylo nějaké AlbumId obdrženo - pokud ne, nezobrazí se nic
                AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba
                PicturesInAlbum = _context.StoredFileAlbums.Include(a => a.Album).Where(a => a.AlbumId == albumid).Count(); // počet fotek v albu
                PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Album).Include(b => b.Picture).Where(a => a.AlbumId == albumid).OrderBy(a => a.Picture.OriginalName).ToListAsync(); // fotky v konkrétním albu
                IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();
            }
            else if (albumid != null && filterid == 2)
            {
                UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
                Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                AlbumIdReceived = albumid; // oveření do PageModel, jestli bylo nějaké AlbumId obdrženo - pokud ne, nezobrazí se nic
                AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba
                PicturesInAlbum = _context.StoredFileAlbums.Include(a => a.Album).Where(a => a.AlbumId == albumid).Count(); // počet fotek v albu
                PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Album).Include(b => b.Picture).Where(a => a.AlbumId == albumid).OrderBy(a => a.Picture.UploadedAt).ToListAsync(); // fotky v konkrétním albu
                IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();
            }
        }

        public async Task<IActionResult> OnPostDeleteAlbum(Guid? albumid) // Tlačítko smazat album - done
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
            Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
            UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele

            if (Album.UploaderId == UserId)
            {
                if (albumid == null && albumid != AlbumIdDefault)
                {
                    return RedirectToPage(new { albumid = albumid });
                }
                else if (albumid != null && albumid == AlbumIdDefault)
                {
                    return RedirectToPage(new {albumid = albumid});
                }
                else if (albumid != null && albumid != AlbumIdDefault)
                {
                    // smazat album
                    Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
                    _context.Albums.Remove(Album);
                    await _context.SaveChangesAsync();
                    return RedirectToPage("../Index");
                }
            }
            else
            {
                return RedirectToPage(new { albumid = albumid });
            }
            return RedirectToPage(new {albumid = albumid});
        }

        public async Task<IActionResult> OnPostMakeAlbumPrivate(Guid? albumid) // Tlačítko nastavit jako soukromé - done
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
            IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();
            Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
            UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele

            if (Album.UploaderId == UserId) // pokud vlastním album
            {
                if (albumid == null && albumid != AlbumIdDefault) 
                {
                    return RedirectToPage(new { albumid = albumid });
                }
                else if (albumid != null && albumid == AlbumIdDefault)
                {
                    return RedirectToPage(new { albumid = albumid });
                }
                else if (albumid != null && albumid != AlbumIdDefault && IsAlbumPublic == true)
                {
                    Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync(); // album se nastaví na soukromé
                    Album.IsPublic = false;
                    await _context.SaveChangesAsync();

                    //Pictures = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == userId).ToListAsync();
                    PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Picture).Include(b => b.Album).Where(b => b.AlbumId == albumid).ToListAsync(); // všechny záznamy fotek v konkrétním albu
                    foreach (var item in PicturesAlbum) // nastavit pouze fotky, které vlastním
                    {
                        if (item.Picture.UploaderId == userId)
                        {
                            //PictureAlbum = await _context.StoredFileAlbums.Include(a => a.Picture).Include(b => b.Album).Where(a => a.PictureId == item.Id).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync(); // všechny záznamy uživatelových obrázků v konkrétním albu
                            Picture = await _context.Files.Include(a => a.Uploader).Include(b => b.StoredFileAlbums).Where(b => b.Id == item.PictureId).FirstOrDefaultAsync();
                            Picture.IsPublic = false;
                            await _context.SaveChangesAsync();
                        }
                        else
                        {

                        }

                    }

                    //PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Album).Where(a => a.AlbumId == albumid).ToListAsync(); // list všech záznamů obrázku z určitého alba
                    //foreach (var item in PicturesAlbum)
                    //{
                    //    Picture = await _context.Files.Include(a => a.Uploader).Include(b => b.StoredFileAlbums).Where(a => a.UploaderId == userId).Where(b => b.Id == item.PictureId).FirstOrDefaultAsync();
                    //    Picture.IsPublic = false;
                    //    await _context.SaveChangesAsync();
                    //}
                    return RedirectToPage(new { albumid = albumid });
                }
                return RedirectToPage(new { albumid = albumid });
            }
            else
            {
                return RedirectToPage("../Index");
            }
            return RedirectToPage(new { albumid = albumid});
            
        }

        public async Task<IActionResult> OnPostMakeAlbumPublic(Guid? albumid) // Tlačítko nastavit jako veřejné
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
            IsAlbumPublic = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Select(c => c.IsPublic).FirstOrDefaultAsync();
            Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();
            UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele

            if (Album.UploaderId == UserId)
            {
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
                    UserId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele

                    PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Picture).Include(b => b.Album).Where(b => b.AlbumId == albumid).ToListAsync(); // všechny záznamy fotek v konkrétním albu
                    foreach (var item in PicturesAlbum) // nastavit pouze fotky, které vlastním
                    {
                        if (item.Picture.UploaderId == userId)
                        {
                            //PictureAlbum = await _context.StoredFileAlbums.Include(a => a.Picture).Include(b => b.Album).Where(a => a.PictureId == item.Id).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync(); // všechny záznamy uživatelových obrázků v konkrétním albu
                            Picture = await _context.Files.Include(a => a.Uploader).Include(b => b.StoredFileAlbums).Where(b => b.Id == item.PictureId).FirstOrDefaultAsync();
                            Picture.IsPublic = true;
                            await _context.SaveChangesAsync();
                        }
                        else
                        {

                        }
                    }
                    return RedirectToPage(new { albumid = albumid });
                }
                return RedirectToPage(new { albumid = albumid });
            }
            else
            {
                return RedirectToPage("../Index");
            }

        }

        public async Task<IActionResult> OnPostChangeName(Guid? albumid) // tlačítko změnit název alba
        {
            if (albumid != null)
            {
                var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
                AlbumIdDefault = _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).Where(b => b.IsDefault == true).Select(b => b.AlbumId).FirstOrDefault(); // AlbumId výchozího alba - nelze ho smazat
                if (albumid != AlbumIdDefault)
                {
                    Album = await _context.Albums.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.AlbumId == albumid).FirstOrDefaultAsync();

                    Album.Name = ChangeName;
                    await _context.SaveChangesAsync();
                    return RedirectToPage(new { albumid = albumid });
                }
                else
                {
                    return RedirectToPage(new { albumid = albumid });
                }
            }
            else
            {
                return RedirectToPage(new { albumid = albumid });
            }
            return RedirectToPage(new { albumid = albumid });
        }
        }
    }


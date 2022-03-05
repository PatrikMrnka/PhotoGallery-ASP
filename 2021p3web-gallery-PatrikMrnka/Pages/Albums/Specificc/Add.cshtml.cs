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
    public class AddModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public AddModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public string PictureName { get; set; }
        public string NotFoundPicture { get; set; }
        public int PicturesInAlbum { get; set; }
        public bool IsAlbumPublic { get; set; }
        public Guid? PictureIdReceived { get; set; }
        public Guid? AlbumIdDefault { get; set; }
        public Album Album { get; set; }
        public IList<Album> Albums { get; set; }
        public StoredFile Picture { get; set; }
        public IList<StoredFile> Pictures { get; set; }
        public StoredFileAlbum PictureAlbum { get; set; }
        [BindProperty]
        public IList<StoredFileAlbum> PicturesAlbum { get; set; }

        public async Task OnGetAsync(Guid? pictureid)
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // ID přihlášeného uživatele
            PictureIdReceived = pictureid;

            if (pictureid == null)
            {
                NotFoundPicture = "Nebyla zvolena konkrétní fotla.";
            }
            else if (pictureid != null)
            {
                PictureName = await _context.Files.Include(a => a.Uploader).Where(a => a.UploaderId == userId).Where(b => b.Id == pictureid).Select(b => b.OriginalName).FirstOrDefaultAsync();
                PicturesAlbum = await _context.StoredFileAlbums.Include(a => a.Picture).Where(a => a.PictureId != pictureid).ToListAsync(); // všechna alba, kde obrázek nahraný není
            }
        }
    }
}

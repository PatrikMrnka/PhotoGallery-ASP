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

namespace _2021p3web_gallery_PatrikMrnka.Pages.Albums.Specific
{
    public class IndexModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public IndexModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public string? AlbumName { get; set; }
        public int PictureInAlbumCount { get; set; }
        public string ErrorMessage { get; set; }
        public Guid? AlbumId { get; set; }
        public IList<StoredFileAlbum> PictureId { get; set; }
        public IList<StoredFileAlbum> Pictures { get; set; }

        public async Task OnGetAsync(Guid? id, string? name)
        {
            AlbumId = id;
            if (id == null && name == null)
            {
                AlbumName = "Neidentifikováno";
                PictureInAlbumCount = 0;
                ErrorMessage = "Není odkazováno na určité album. Vyberte na stránce alba určité album";
            }
            else if (id != null && name != null)
            {
                AlbumName = name;
                PictureInAlbumCount = _context.StoredFileAlbums.Include(a => a.Picture).Where(b => b.AlbumId == id).Count();
                Pictures = await _context.StoredFileAlbums.Include(a => a.Picture).Where(b => b.AlbumId == id).ToListAsync();
            }
        }
    }
}

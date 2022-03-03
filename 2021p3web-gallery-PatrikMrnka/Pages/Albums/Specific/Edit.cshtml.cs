#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using _2021p3web_gallery_PatrikMrnka.Data;
using _2021p3web_gallery_PatrikMrnka.Models;

namespace _2021p3web_gallery_PatrikMrnka.Pages.Albums.Specific
{
    public class EditModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public EditModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public StoredFileAlbum StoredFileAlbum { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            StoredFileAlbum = await _context.StoredFileAlbums
                .Include(s => s.Album)
                .Include(s => s.Picture).FirstOrDefaultAsync(m => m.PictureId == id);

            if (StoredFileAlbum == null)
            {
                return NotFound();
            }
           ViewData["AlbumId"] = new SelectList(_context.Albums, "AlbumId", "Name");
           ViewData["PictureId"] = new SelectList(_context.Files, "Id", "ContentType");
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(StoredFileAlbum).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StoredFileAlbumExists(StoredFileAlbum.PictureId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool StoredFileAlbumExists(Guid? id)
        {
            return _context.StoredFileAlbums.Any(e => e.PictureId == id);
        }
    }
}

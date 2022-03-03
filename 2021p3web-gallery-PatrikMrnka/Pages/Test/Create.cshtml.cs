#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using _2021p3web_gallery_PatrikMrnka.Data;
using _2021p3web_gallery_PatrikMrnka.Models;

namespace _2021p3web_gallery_PatrikMrnka.Pages.Test
{
    public class CreateModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public CreateModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
        ViewData["AlbumId"] = new SelectList(_context.Albums, "AlbumId", "Name");
        ViewData["PictureId"] = new SelectList(_context.Files, "Id", "ContentType");
            return Page();
        }

        [BindProperty]
        public StoredFileAlbum StoredFileAlbum { get; set; }

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.StoredFileAlbums.Add(StoredFileAlbum);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}

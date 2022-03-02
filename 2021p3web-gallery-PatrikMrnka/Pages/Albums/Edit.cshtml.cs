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
using System.Security.Claims;

namespace _2021p3web_gallery_PatrikMrnka.Pages.Albums
{
    public class EditModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public EditModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Album Album { get; set; }
        public string userId { get; set; }
        public Guid UploaderId { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Album = await _context.Albums
                .Include(a => a.Uploader).FirstOrDefaultAsync(m => m.AlbumId == id);

            userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
            //UploaderId = _context.Albums.Where(a => a.AlbumId == id).Where(a => a.UploaderId == userId).FirstOrDefault();

            if (Album == null)
            {
                return RedirectToPage("./Index");
            }

            //if (UploaderId != userId)
            //{
            //    return RedirectToPage("./Index");
            //}
            else if (UploaderId == null)
            {
                return RedirectToPage("./Index");
            }

            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            //if (!ModelState.IsValid)
            //{
            //    return Page();
            //}

            _context.Attach(Album).State = EntityState.Modified;

            try
            {
                var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
                var newAlbum = new Album
                {
                    Name = Album.Name,
                    IsPublic = Album.IsPublic,
                    CreationDate = Album.CreationDate,
                    UploaderId = userId
                };
                _context.Albums.Add(newAlbum);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AlbumExists(Album.AlbumId))
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

        private bool AlbumExists(Guid id)
        {
            return _context.Albums.Any(e => e.AlbumId == id);
        }
    }
}

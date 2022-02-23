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
using System.Security.Claims;

namespace _2021p3web_gallery_PatrikMrnka.Pages.Albums
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
        //ViewData["UploaderId"] = new SelectList(_context.Users, "Id", "Id");
            return Page();
        }

        [BindProperty]
        public Album Album { get; set; }

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;   

            //if (!ModelState.IsValid)
            //{
            //    return Page();
            //}

            var newAlbum = new Album
            {
                Name = Album.Name,
                CreationDate = DateTime.Now,
                IsPublic = false,
                UploaderId = userId
            };

            _context.Albums.Add(newAlbum);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}

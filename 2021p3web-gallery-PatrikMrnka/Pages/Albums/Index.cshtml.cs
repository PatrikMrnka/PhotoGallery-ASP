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

namespace _2021p3web_gallery_PatrikMrnka.Pages.Albums
{
    public class IndexModel : PageModel
    {
        private readonly _2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext _context;

        public IndexModel(_2021p3web_gallery_PatrikMrnka.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Album> Album { get;set; }
        public string? userId { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
            if(userId == null)
            {
                Album = await _context.Albums
                    .Include(a => a.Uploader).Where(a => a.IsPublic == true).ToListAsync();
            }
            else
            {
                Album = await _context.Albums
                    .Include(a => a.Uploader).Where(a => a.UploaderId == userId).ToListAsync();
            }
            //Album = await _context.Albums
            //    .Include(a => a.Uploader).ToListAsync();
        }
    }
}

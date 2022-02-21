using _2021p3web_gallery_PatrikMrnka.Data;
using _2021p3web_gallery_PatrikMrnka.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp.Formats;
using System.Security.Claims;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.EntityFrameworkCore;

namespace _2021p3web_gallery_PatrikMrnka.Pages
{
    public class UploadModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private ApplicationDbContext _context;
        private IConfiguration _configuration;
        private int _squareSize;
        private int _sameAspectRatioHeigth;

        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        public List<string> Files { get; set; } = new List<string>();
        [BindProperty]
        public ICollection<IFormFile> Upload { get; set; }
        public bool PictureIsPublic { get; set; }
        public UploadModel(IWebHostEnvironment environment, ApplicationDbContext context, IConfiguration configuration)
        {
            _environment = environment;
            _context = context;
            _configuration = configuration;
            if (Int32.TryParse(_configuration["Thumbnails:SquareSize"], out _squareSize) == false) _squareSize = 64; // získej data z konfigurace nebo použij 64
            if (Int32.TryParse(_configuration["Thumbnails:SameAspectRatioHeigth"], out _sameAspectRatioHeigth) == false) _sameAspectRatioHeigth = 128;

        }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // získáme id pøihlášeného uživatele
            int successfulProcessing = 0;
            int failedProcessing = 0;
            foreach(var uploadedFile in Upload) // cyklus pro každý nahrávaný soubor
            {
                var fileRecord = new Picture
                {
                    OriginalName = uploadedFile.FileName,
                    UploaderId = userId,
                    UploadedAt = DateTime.Now,
                    ContentType = uploadedFile.ContentType,
                    IsPublic = PictureIsPublic
                };
                if (uploadedFile.ContentType.StartsWith("image")) // je soubor obrázek?
                {
                    fileRecord.Thumbnails = new List<Thumbnail>();
                    MemoryStream ims = new MemoryStream(); // proud pro pøíchozí obrázek
                    MemoryStream oms1 = new MemoryStream(); // proud pro ètvercový náhled
                    MemoryStream oms2 = new MemoryStream(); // proud pro obdélníkový náhled
                    uploadedFile.CopyTo(ims); // vlož obsah do vstupního proudu
                    IImageFormat format; // zde si uložíme formát obrázku (JPEG, GIF, ...), budeme ho potøebovat pøi ukládání
                    using (Image image = Image.Load(ims.ToArray(), out format)) // vytvoøíme ètvercový náhled
                    {
                        int largestSize = Math.Max(image.Height, image.Width);
                        if (image.Width > image.Height) // podle orientace zmìníme velikost obrázku
                        {
                            image.Mutate(x => x.Resize(0, _squareSize));
                        }
                        else
                        {
                            image.Mutate(x => x.Resize(_squareSize, 0));
                        }
                        image.Mutate(x => x.Crop(new Rectangle((image.Width - _squareSize) / 2, (image.Height - _squareSize) / 2, _squareSize, _squareSize))); // obrázek oøízneme na ètverec
                        image.Save(oms1, format); // vložime ho do výstupního proudu
                        fileRecord.Thumbnails.Add(new Thumbnail { Type = ThumbnailType.Square, Blob = oms1.ToArray() }); // uložíme do databáze jako pole bytù
                    }
                    using (Image image = Image.Load(ims.ToArray(), out format)) // obdélníkový náhled zaèíná zde
                    {
                        image.Mutate(x => x.Resize(0, _sameAspectRatioHeigth)); // zmìna velikosti
                        image.Save(oms2, format); // pøes proud ho uložit do databáze
                        fileRecord.Thumbnails.Add(new Thumbnail { Type = ThumbnailType.SameAspectRatio, Blob = oms2.ToArray() });
                    }
                }
                try
                {
                    _context.Pictures.Add(fileRecord); // uložení záznamu do databáze
                    await _context.SaveChangesAsync(); // vygeneruje se jeho klíè ve formátu Guid
                    var file = Path.Combine(_environment.ContentRootPath, "Uploads", fileRecord.Id.ToString());
                    // pod tímto klíèem se soubor uloží i fyzicky na disk
                    using (var fileStream = new FileStream(file, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(fileStream);
                    };
                    successfulProcessing++;
                }
                catch
                {
                    failedProcessing++;
                }
                if(failedProcessing == 0)
                {
                    SuccessMessage = "Všechny soubory byly úspìšnì nahrány.";
                }
                else
                {
                    ErrorMessage = "Je zde <b>" + failedProcessing + "<b> problém/y pøi nahrávání a zpracování souborù.";
                }
            }
            return RedirectToPage("/Index");
        }
    }
}

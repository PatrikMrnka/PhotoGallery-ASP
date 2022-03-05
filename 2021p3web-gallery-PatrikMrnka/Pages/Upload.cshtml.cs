using _2021p3web_gallery_PatrikMrnka.Data;
using _2021p3web_gallery_PatrikMrnka.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System.Security.Claims;

namespace _2021p3web_gallery_PatrikMrnka.Pages
{
    public class UploadModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private ApplicationDbContext _context;
        private IConfiguration _configuration;
        private int _squareSize;
        private int _sameAspectRatioHeigth;

        public Guid AlbumIdDefault { get; set; }
        public Guid? AlbumIdReceived { get; set; }
        public Guid? PictureIdReceived { get; set; }
        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        public List<string> Files { get; set; } = new List<string>();
        public ICollection<IFormFile> Upload { get; set; }
        [BindProperty]
        public StoredFile StoredFile { get; set; }
        [BindProperty]
        public Album Album { get; set; }
        [BindProperty]
        public StoredFileAlbum StoredFileAlbum { get; set; }


        public UploadModel(IWebHostEnvironment environment, ApplicationDbContext context, IConfiguration configuration)
        {
            _environment = environment;
            _context = context;
            _configuration = configuration;
            if (Int32.TryParse(_configuration["Thumbnails:SquareSize"], out _squareSize) == false) _squareSize = 320; // získej data z konfigurace nebo použij 64
            if (Int32.TryParse(_configuration["Thumbnails:SameAspectRatioHeigth"], out _sameAspectRatioHeigth) == false) _sameAspectRatioHeigth = 320;

        }
        public void OnGet(Guid? albumid, Guid? pictureid)
        {
            AlbumIdReceived = albumid;
            PictureIdReceived = pictureid;
            //ViewData["AlbumId"] = new SelectList(_context.StoredFileAlbums, "AlbumId", "Name");
        }

        public async Task<IActionResult> OnPostAsync(Guid? albumid, Guid? pictureid)
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value; // získáme id pøihlášeného uživatele
            int successfulProcessing = 0;
            int failedProcessing = 0;
            AlbumIdReceived = albumid; // ID alba do kterého chci pøidat obrázek
            PictureIdReceived = pictureid; // ID obrázku, který chci nahrát do nìjakého alba
            foreach(var uploadedFile in Upload) // cyklus pro každý nahrávaný soubor
            {
                if (uploadedFile.ContentType.StartsWith("image")) // je soubor obrázek?
                {
                    MemoryStream ims1 = new MemoryStream(); // proud pro pøíchozí obrázek
                    uploadedFile.CopyTo(ims1);
                    using (Image image1 = Image.Load(ims1.ToArray()))
                    {
                        if (image1.Width <= 2000) // je šíøka fotky menší nebo rovna 2000px?
                        { 
                            var fileRecord = new StoredFile
                            {
                                OriginalName = uploadedFile.FileName,
                                UploaderId = userId,
                                UploadedAt = DateTime.Now,
                                ContentType = uploadedFile.ContentType,
                                IsPublic = StoredFile.IsPublic
                            };

                            fileRecord.Thumbnails = new List<Thumbnail>();

                            MemoryStream ims = new MemoryStream(); // proud pro pøíchozí obrázek
                            MemoryStream oms1 = new MemoryStream(); // proud pro ètvercový náhled
                            MemoryStream oms2 = new MemoryStream(); // proud pro obdélníkový náhled
                            IImageFormat format; // zde si uložíme formát obrázku (JPEG, GIF, ...), budeme ho potøebovat pøi ukládání
                            uploadedFile.CopyTo(ims); // vlož obsah do vstupního proudu
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
                            try
                            {

                                _context.Files.Add(fileRecord); // uložení záznamu do databáze
                                await _context.SaveChangesAsync(); // vygeneruje se jeho klíè ve formátu Guid
                                AlbumIdDefault = _context.Albums.Where(a => a.UploaderId == userId).Where(a => a.IsDefault == true).Select(a => a.AlbumId).FirstOrDefault(); // ID výchozího 

                                var file = Path.Combine(_environment.ContentRootPath, "Uploads", fileRecord.Id.ToString());
                                // pod tímto klíèem se soubor uloží i fyzicky na disk
                                using (var fileStream = new FileStream(file, FileMode.Create))
                                {
                                    await uploadedFile.CopyToAsync(fileStream);
                                };
                                successfulProcessing++;

                                if (albumid == null && albumid != AlbumIdDefault) // uložení fotky do výchozího alba
                                {
                                    AlbumIdDefault = _context.Albums.Where(a => a.UploaderId == userId).Where(a => a.IsDefault == true).Select(a => a.AlbumId).FirstOrDefault(); // ID výchozího alba

                                    var albumPictureRecord = new StoredFileAlbum
                                    {
                                        AlbumId = AlbumIdDefault,
                                        PictureId = fileRecord.Id
                                    };

                                    _context.StoredFileAlbums.Add(albumPictureRecord); 
                                    await _context.SaveChangesAsync();
                                }
                                else if (albumid != null && albumid != AlbumIdDefault) // uložení fotky do konkrétního alba - uloží se i do výchozího
                                {
                                    AlbumIdDefault = _context.Albums.Where(a => a.UploaderId == userId).Where(a => a.IsDefault == true).Select(a => a.AlbumId).FirstOrDefault(); // ID výchozího alba
                                    var albumPictureRecordDefault = new StoredFileAlbum // uložení do výchozího alba
                                    {
                                        AlbumId = AlbumIdDefault,
                                        PictureId = fileRecord.Id
                                    };

                                    _context.StoredFileAlbums.Add(albumPictureRecordDefault);
                                    await _context.SaveChangesAsync();

                                    var albumPictureRecord = new StoredFileAlbum // uložení do konkrétního alba
                                    {
                                        AlbumId = AlbumIdReceived,
                                        PictureId = fileRecord.Id
                                    };

                                    _context.StoredFileAlbums.Add(albumPictureRecord);
                                    await _context.SaveChangesAsync();

                                    return RedirectToPage("./Albums/Specificc/Index", new {albumid = albumid}); // vrátí se zpátky na album - passing parameter
                                }
                                else if (albumid != null && albumid == AlbumIdDefault) // uložení do výchozího alba
                                {
                                    AlbumIdDefault = _context.Albums.Where(a => a.UploaderId == userId).Where(a => a.IsDefault == true).Select(a => a.AlbumId).FirstOrDefault();

                                    var albumPictureRecord = new StoredFileAlbum
                                    {
                                        AlbumId = AlbumIdDefault,
                                        PictureId = fileRecord.Id
                                    };

                                    _context.StoredFileAlbums.Add(albumPictureRecord);
                                    await _context.SaveChangesAsync();
                                }
                            }
                            catch
                            {
                                failedProcessing++;
                            }
                            if (failedProcessing == 0)
                            {
                                SuccessMessage = "Všechny soubory byly úspìšnì nahrány.";
                            }
                            else
                            {
                                ErrorMessage = "Je zde <b>" + failedProcessing + "<b> problém/y pøi nahrávání a zpracování souborù.";
                            }
                        }
                        else if (image1.Width > 2000)
                        {
                            ErrorMessage = "Obrázek mùže být maximálnì široký 2000px";
                        }
                    }
                }
                else if (!uploadedFile.ContentType.StartsWith("image"))
                {
                    ErrorMessage = "Nahraný soubor není obrázek";
                }
            }
            return RedirectToPage("/Index");
        }
    }
}

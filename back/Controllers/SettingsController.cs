using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/settings
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _context.SiteSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SiteSettings();
                _context.SiteSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return Ok(settings);
        }

        // PUT: api/settings
        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] SiteSettings updatedSettings)
        {
            var settings = await _context.SiteSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SiteSettings();
                _context.SiteSettings.Add(settings);
            }

            settings.Address = updatedSettings.Address;
            settings.Email = updatedSettings.Email;
            settings.Phone = updatedSettings.Phone;
            settings.BusinessHours = updatedSettings.BusinessHours;
            settings.FacebookUrl = updatedSettings.FacebookUrl;
            settings.InstagramUrl = updatedSettings.InstagramUrl;
            settings.WhatsAppUrl = updatedSettings.WhatsAppUrl;
            settings.TikTokUrl = updatedSettings.TikTokUrl;
            settings.AboutUsText = updatedSettings.AboutUsText;
            settings.SizeGuideData = updatedSettings.SizeGuideData;
            settings.HeroSliderImages = updatedSettings.HeroSliderImages ?? settings.HeroSliderImages;
            settings.CraftSliderImages = updatedSettings.CraftSliderImages ?? settings.CraftSliderImages;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Settings updated successfully", settings });
        }
    }
}

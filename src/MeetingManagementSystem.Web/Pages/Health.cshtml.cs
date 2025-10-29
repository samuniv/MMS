using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.Web.Pages
{
    public class HealthModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HealthModel> _logger;
        private readonly IConfiguration _configuration;

        public string Status { get; set; } = "Unhealthy";

        public HealthModel(
            ApplicationDbContext context,
            ILogger<HealthModel> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var healthChecksEnabled = _configuration.GetValue<bool>("HealthChecks:Enabled", true);
                
                if (!healthChecksEnabled)
                {
                    Status = "Healthy";
                    return Content(Status, "text/plain");
                }

                // Check database connectivity
                var databaseCheckEnabled = _configuration.GetValue<bool>("HealthChecks:DatabaseCheckEnabled", true);
                if (databaseCheckEnabled)
                {
                    await _context.Database.CanConnectAsync();
                    await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                }

                // Check disk space
                var diskSpaceCheckEnabled = _configuration.GetValue<bool>("HealthChecks:DiskSpaceCheckEnabled", true);
                if (diskSpaceCheckEnabled)
                {
                    var uploadsPath = _configuration.GetValue<string>("DocumentSettings:UploadPath", "uploads");
                    var logsPath = "logs";
                    
                    CheckDiskSpace(uploadsPath);
                    CheckDiskSpace(logsPath);
                }

                Status = "Healthy";
                return Content(Status, "text/plain");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                Status = "Unhealthy";
                Response.StatusCode = 503;
                return Content(Status, "text/plain");
            }
        }

        private void CheckDiskSpace(string path)
        {
            try
            {
                var minimumFreeSpaceGB = _configuration.GetValue<long>("HealthChecks:MinimumFreeDiskSpaceGB", 5);
                
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path)) ?? "C:\\");
                var freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);

                if (freeSpaceGB < minimumFreeSpaceGB)
                {
                    _logger.LogWarning("Low disk space: {FreeSpaceGB}GB available on {DriveName}", freeSpaceGB, drive.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check disk space for path: {Path}", path);
            }
        }
    }
}

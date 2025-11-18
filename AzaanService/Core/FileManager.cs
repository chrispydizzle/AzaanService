namespace AzaanService.Core
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;

    internal class FileManager : IFileManager
    {
        private readonly Random r = new();
        private readonly string[] files;

        public FileManager(IConfiguration configuration, ILogger<IFileManager> logger)
        {
            try
            {
                this.files = Directory.GetFiles(configuration["azaan:source"] ?? string.Empty, "*.opus");
                for (var i = 0; i < this.files.Length; i++)
                {
                    this.files[i] = $"{configuration["azaan:urlpath"]}/{Path.GetFileName(this.files[i])}";
                }
            }
            catch (DirectoryNotFoundException fex)
            {
                logger.LogWarning(fex, "Primary source not found, attempting fallback");
                try
                {
                    this.files = Directory.GetFiles(configuration["azaan:backupsource"] ?? string.Empty, "*.opus");
                    logger.LogInformation("Backup lookup successful");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Fallback also failed, no files available");
                    this.files = [];
                    throw;
                }
            }

            for (var i = 0; i < this.files.Length; i++)
            {
                this.files[i] = $"{configuration["azaan:urlpath"]}/{Path.GetFileName(this.files[i])}";
            }
        }

        public string Pick()
        {
            return this.files.Length == 0 ? string.Empty : this.files[this.r.Next(0, this.files.Length)];
        }
    }

    public interface IFileManager
    {
        string Pick();
    }
}

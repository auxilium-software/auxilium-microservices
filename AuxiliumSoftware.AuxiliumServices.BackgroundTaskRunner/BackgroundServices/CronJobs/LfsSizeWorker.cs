using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Superclasses;
using AuxiliumSoftware.AuxiliumServices.Common.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.BackgroundServices.CronJobs
{
    public class LfsSizeWorker : CronBackgroundService
    {
        private const string Schedule = "0 * * * *"; // every hour on the hour

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LfsSizeWorker> _logger;
        private readonly IConfiguration _configuration;

        public LfsSizeWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<LfsSizeWorker> logger,
            IConfiguration configuration
            )
            : base(Schedule, TimeZoneInfo.Utc, logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            var settings = sp.GetRequiredService<ISystemSettingsService>();
            var db = sp.GetRequiredService<AuxiliumDbContext>();

            string lfsRoot = _configuration.GetValue<string>("FileSystem:RootStorageDirectories:AuxLFS")!;

            if (!Directory.Exists(lfsRoot))
            {
                _logger.LogWarning("LfsSizeWorker: LFS root '{Root}' does not exist, skipping.", lfsRoot);
                return;
            }

            long totalBytes = await Task.Run(
                () => new DirectoryInfo(lfsRoot)
                          .EnumerateFiles("*", SearchOption.AllDirectories)
                          .Sum(f => f.Length),
                stoppingToken);

            db.System_Metrics.Add(new SystemMetricEntityModel
            {
                Id = UUIDUtilities.GenerateV5(DatabaseObjectTypeEnum.System_MetricEntry),
                CreatedAtUtc = DateTime.UtcNow,
                MetricKey = Common.EntityFramework.Enumerators.SystemMetricKeyEnum.Lfs_SizeBytes,
                MetricValue = totalBytes,
            });

            await db.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "LfsSizeWorker: recorded {Bytes:N0} bytes ({MB:F2} MB) in '{Root}' at {Time}",
                totalBytes, totalBytes / 1_048_576.0, lfsRoot, DateTimeOffset.UtcNow);
        }
    }
}

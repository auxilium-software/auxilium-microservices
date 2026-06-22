using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Superclasses;
using AuxiliumSoftware.AuxiliumServices.Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.BackgroundServices.CronJobs
{
    public class DatabaseSizeWorker : CronBackgroundService
    {
        private const string Schedule = "0 * * * *"; // every hour on the hour

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DatabaseSizeWorker> _logger;

        public DatabaseSizeWorker(IServiceScopeFactory scopeFactory, ILogger<DatabaseSizeWorker> logger)
            : base(Schedule, TimeZoneInfo.Utc, logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuxiliumDbContext>();

            var sizeBytes = await db.Database
                .SqlQueryRaw<long>("""
                SELECT CAST(
                    SUM(data_length + index_length) AS SIGNED
                ) AS Value
                FROM information_schema.tables
                WHERE table_schema = DATABASE()
                """)
                .SingleAsync(stoppingToken);

            db.System_Metrics.Add(new SystemMetricEntityModel
            {
                Id = UUIDUtilities.GenerateV5(DatabaseObjectTypeEnum.System_MetricEntry),
                CreatedAtUtc = DateTime.UtcNow,
                MetricKey = Common.EntityFramework.Enumerators.SystemMetricKeyEnum.Db_SizeBytes,
                MetricValue = sizeBytes,
            });

            await db.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "DatabaseSizeWorker: recorded {Bytes:N0} bytes ({MB:F2} MB) at {Time}",
                sizeBytes, sizeBytes / 1_048_576.0, DateTimeOffset.UtcNow);
        }
    }
}

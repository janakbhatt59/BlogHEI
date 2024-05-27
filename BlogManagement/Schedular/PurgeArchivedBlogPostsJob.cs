using BlogManagement.Controllers;
using BlogManagement.DBContext;
using BlogManagement.Models.Entity;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlogManagement.Schedular
{
    public class PurgeArchivedBlogPostsJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;

        public PurgeArchivedBlogPostsJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<PurgeArchivedBlogPostsJob>>();

                var retentionPeriod = TimeSpan.FromDays(30);
                var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);

                var lastExecutionTime = dbContext.SchedulerConfigs.FirstOrDefault()?.LastExecutionTime ?? DateTime.MinValue;
                if (DateTime.UtcNow.Subtract(lastExecutionTime) < TimeSpan.FromDays(1))
                {
                    logger.LogInformation("Job skipped. It's not time yet.");
                    return;
                }

                var archivedPosts = dbContext.Blog.Where(p => p.IsDeleted && p.CreatedDate < cutoffDate);

                dbContext.Blog.RemoveRange(archivedPosts);
                await dbContext.SaveChangesAsync();

                logger.LogInformation($"Purged {archivedPosts.Count()} archived blog posts older than {retentionPeriod.Days} days.");

                if (dbContext.SchedulerConfigs.Any())
                {
                    var config = dbContext.SchedulerConfigs.First();
                    config.LastExecutionTime = DateTime.UtcNow;
                }
                else
                {
                    dbContext.SchedulerConfigs.Add(new SchedulerConfig { LastExecutionTime = DateTime.UtcNow });
                }
                await dbContext.SaveChangesAsync();
            }
        }
    }
}

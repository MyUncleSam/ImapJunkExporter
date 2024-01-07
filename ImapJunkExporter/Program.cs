using ImapJunkExporter.Config;
using Microsoft.Extensions.Configuration;
using NLog;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Quartz;


namespace ImapJunkExporter;
internal class Program
{
    private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

    private async static Task Main()
    {
        try
        {
            logger.Info("Starting {ProgramName}", System.AppDomain.CurrentDomain.FriendlyName);
            logger.Debug("Loading configuiration");
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            logger.Debug("\t- extracting Worker");
            WorkerOption workerConfig = config.GetSection("Worker").Get<WorkerOption>()!;

            logger.Debug("\t- extracting Mailboxes");
            IEnumerable<MailboxesOption> mailboxesConfig = config.GetSection("Mailboxes").Get<IEnumerable<MailboxesOption>>()!;

            logger.Debug("\t- extracting Cron");
            ScheduleOptions scheduleConfig = config.GetSection("Schedule").Get<ScheduleOptions>()!;

            logger.Debug("Loading dependency injection");
            var jobKey = new JobKey("workerJob", "workerGroup");
            var triggerKey = new TriggerKey("workerTrigger", "workerGroup");
            var servicesCollection = new ServiceCollection()
                .AddSingleton(workerConfig)
                .AddSingleton(mailboxesConfig)
                .AddSingleton(scheduleConfig)
                .AddTransient<IWorkerJob, WorkerJob>()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog(config);
                }); ;

            if(scheduleConfig.RunOnce)
            {
                logger.Debug("RunOnce - starting one time export");
                logger.Debug("\t- Building service provider");
                using var serviceProvider = servicesCollection.BuildServiceProvider();

                logger.Debug("\t- Get required worker");
                var worker = serviceProvider.GetRequiredService<IWorkerJob>();

                logger.Debug("\t- Run worker job");
                await worker.Execute();
            }
            else
            {
                logger.Debug("Loading quartz scheduler into dependency injection");
                using var serviceProvider = servicesCollection
                    .AddQuartz(q =>
                    {
                        q.SchedulerId = "JobScheduler";
                        q.SchedulerName = "JobScheduler";
                        q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 1);
                        q.AddJob<WorkerJob>(j => j.WithIdentity(jobKey));
                        q.AddTrigger(t => t
                            .WithIdentity(triggerKey)
                            .ForJob(jobKey)
                            .StartNow()
                            .WithCronSchedule(scheduleConfig.Cron));
                    })
                    .BuildServiceProvider();

                var schedulerFactory = serviceProvider.GetRequiredService<ISchedulerFactory>();
                var scheduler = await schedulerFactory.GetScheduler();

                await scheduler.Start();

                while(true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                logger.Error(ex, "GLOBAL ERROR");
            }
            catch
            {
                Console.WriteLine($"GLOBAL ERROR");
                Console.WriteLine(ex.ToString());
            }
        }
        finally
        {
            try
            {
                logger.Info("Stopped {ProgramName}", System.AppDomain.CurrentDomain.FriendlyName);
            }
            catch { }
        }
    }
}
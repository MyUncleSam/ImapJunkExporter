using ImapJunkExporter.Config;
using Microsoft.Extensions.Configuration;
using NLog;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;
using NCrontab;


namespace ImapJunkExporter;
internal class Program
{
    private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

    private static bool UserCanceled { get; set; } = false;

    private async static Task Main()
    {
        Console.CancelKeyPress += Console_CancelKeyPress;

        try
        {
            logger.Info("Starting {ProgramName}", System.AppDomain.CurrentDomain.FriendlyName);

            logger.Debug("\t- initial preperations");
            Setup();

            logger.Debug("\t- Loading configuiration");
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            logger.Debug("\t\t- extracting Worker");
            WorkerOption workerConfig = config.GetSection("Worker").Get<WorkerOption>()!;

            logger.Debug("\t\t- extracting Mailboxes");
            IEnumerable<MailboxesOption> mailboxesConfig = config.GetSection("Mailboxes").Get<IEnumerable<MailboxesOption>>()!;

            logger.Debug("\t\t- extracting Cron");
            CronOptions cronConfig = config.GetSection("Cron").Get<CronOptions>()!;

            logger.Debug("\tLoading dependency injection");
            using var servicesProvider = new ServiceCollection()
                .AddSingleton(workerConfig)
                .AddSingleton(mailboxesConfig)
                .AddSingleton(cronConfig)
                .AddTransient<IWorkerJob, WorkerJob>()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog(config);
                })
                .BuildServiceProvider();

            logger.Debug("\t- Get required worker");
            var worker = servicesProvider.GetRequiredService<IWorkerJob>();

            logger.Debug("\t- Loading cron");
            var schedule = CrontabSchedule.Parse(cronConfig.Schedule);

            logger.Info("First run: {NextRun}", schedule.GetNextOccurrence(DateTime.Now));

            while(!UserCanceled) // run till user cancelled the run
            {
                if (workerConfig.RunOnce || schedule.GetNextOccurrence(DateTime.Now) <= DateTime.Now)
                {
                    logger.Info("Worker started");

                    try
                    {
                        await worker.Run();
                    }
                    catch(Exception ex)
                    {
                        logger.Error(ex, "unhandled error during worker run");
                    }
                    finally
                    {
                        logger.Info("Next run: {NextRun}", schedule.GetNextOccurrence(DateTime.Now));
                    }
                }

                if(workerConfig.RunOnce)
                {
                    break;
                }

                if (!workerConfig.RunOnce)
                {
                    // only check once per second
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            logger.Info("Stopped {ProgramName}", System.AppDomain.CurrentDomain.FriendlyName);
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
    }

    private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        logger.Warn("User stopped program. Finishing tasks, please wait.");
        UserCanceled = true;
    }

    private static void Setup()
    {
        if(!Directory.Exists("logging"))
        {
            Directory.CreateDirectory("logging");
        }
    }
}
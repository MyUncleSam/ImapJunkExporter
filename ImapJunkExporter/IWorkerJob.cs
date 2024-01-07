namespace ImapJunkExporter
{
    internal interface IWorkerJob
    {
        public Task Execute();
    }
}

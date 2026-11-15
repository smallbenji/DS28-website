namespace DS.Application.Services.HistoricRegistration
{
    public interface IHistoricRegistrationSyncQueue
    {
        bool IsConfigured { get; }
        void Enqueue();
    }
}

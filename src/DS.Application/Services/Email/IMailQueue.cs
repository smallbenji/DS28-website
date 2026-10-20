namespace DS.Application.Services.Email
{
    public interface IMailQueue
    {
        void Enqueue(string toEmail, string toName, string subject, string body);
    }
}

namespace DS
{
    public class DSSettings
    {
        public string SSO_URL { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string Realm { get; set; }
        public string ConnectionString { get; set; }
        public string SMTPHost { get; set; }
        public string SMTPUser { get; set; }
        public string SMTPPassword { get; set; }
        public string SMTPFromEmail { get; set; }
        public string SMTPFromName { get; set; }
    }
}
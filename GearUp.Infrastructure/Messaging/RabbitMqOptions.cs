namespace GearUp.Infrastructure.Messaging
{
    public sealed class RabbitMqOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string Exchange { get; set; } = "gearup.exchange";
        public string EmailQueue { get; set; } = "gearup.email.queue";
        public string NotificationQueue { get; set; } = "gearup.notification.queue";
        public ushort PrefetchCount { get; set; } = 10;
    }
}


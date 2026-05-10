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
        public string ImageProcessingQueue { get; set; } = "gearup.image.processing.queue";
        public string ImageUploadQueue { get; set; } = "gearup.image.upload.queue";
        public string DeadLetterQueue { get; set; } = "gearup.image.dlq";
        public ushort PrefetchCount { get; set; } = 10;
    }
}


namespace GearUp.Infrastructure.Messaging
{
    public sealed class RabbitMqOptions
    {
        public bool UseRabbitMQ { get; set; }
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string VirtualHost { get; set; } = "/";
        public string Exchange { get; set; } = "gearup.exchange";
        public string EmailQueue { get; set; } = "gearup.email.queue";
        public string NotificationQueue { get; set; } = "gearup.notification.queue";
        public string ImageProcessingQueue { get; set; } = "gearup.image.processing.queue";
        public string ImageUploadQueue { get; set; } = "gearup.image.upload.queue";
        public string DeadLetterQueue { get; set; } = "gearup.dlq";
        public string RetryExchange { get; set; } = "gearup.retry.exchange";
        public string RetryQueue { get; set; } = "gearup.retry.queue";
        public int MaxRetries { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 5000;
        public ushort PrefetchCount { get; set; } = 10;
    }
}


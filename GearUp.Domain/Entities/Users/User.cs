using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Entities.Chats;
using GearUp.Domain.Entities.Posts;
using GearUp.Domain.Entities.RealTime;
using GearUp.Domain.Enums;



namespace GearUp.Domain.Entities.Users
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Provider { get; private set; }
        public string ProviderUserId { get; private set; }
        public string Username { get; private set; }
        public string Email { get; private set; }
        public string Name { get; private set; }
        public string PasswordHash { get; private set; }
        public UserRole Role { get; private set; }
        public DateOnly DateOfBirth { get; private set; }
        public string? PhoneNumber { get; private set; }
        public string AvatarUrl { get; private set; }
        public bool IsEmailVerified { get; private set; }
        public bool IsProfileCompleted { get; private set; }
       
        private readonly List<Post> _posts = new List<Post>();
        private readonly List<CarRental> _ownedRentals = new List<CarRental>();
        private readonly List<CarRental> _bookedRentals = new List<CarRental>();
        private readonly List<Appointment> _receivedAppointments = new List<Appointment>();
        private readonly List<Appointment> _sentAppointments = new List<Appointment>();
        private readonly List<Notification> _notifications = new List<Notification>();
        private readonly List<Car> _cars = new List<Car>();
        private readonly List<ConversationParticipant> _conversationParticipants = new();
        public IReadOnlyCollection<ConversationParticipant> ConversationParticipants => _conversationParticipants.AsReadOnly();

        public IReadOnlyCollection<Post> Posts => _posts.AsReadOnly();
        public IReadOnlyCollection<CarRental> OwnedRentals => _ownedRentals.AsReadOnly();
        public IReadOnlyCollection<CarRental> BookedRentals => _bookedRentals.AsReadOnly();
        public IReadOnlyCollection<Appointment> ReceivedAppointments => _receivedAppointments.AsReadOnly();
        public IReadOnlyCollection<Appointment> SentAppointments => _sentAppointments.AsReadOnly();
        public IReadOnlyCollection<Notification> Notifications => _notifications.AsReadOnly();
        public IReadOnlyCollection<Car> Cars => _cars.AsReadOnly();
        

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private User()
        {
            Id = Guid.NewGuid();
            _bookedRentals = new List<CarRental>();
            _ownedRentals = new List<CarRental>();
            _receivedAppointments = new List<Appointment>();
            _cars = new List<Car>();
            _sentAppointments = new List<Appointment>();
            _conversationParticipants = new List<ConversationParticipant>();
            _notifications = new List<Notification>();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;    
        }

        public static User CreateLocalUser(string username, string email, string name, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash cannot be null or empty.", nameof(passwordHash));

            return new User
            {
               
                Username = username,
                Email = email,
                Name = name,
                PasswordHash = passwordHash,
                IsProfileCompleted = false,
                IsEmailVerified = false,
              
            };
        }

        public static User CreateSocialUser(string provider, string providerUserId)
        {
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provider cannot be null or empty.", nameof(provider));
            if (string.IsNullOrWhiteSpace(providerUserId))
                throw new ArgumentException("Provider user ID cannot be null or empty.", nameof(providerUserId));
            return new User
            {
               
                Provider = provider,
                ProviderUserId = providerUserId,
                IsProfileCompleted = false,
                IsEmailVerified = false,
                
            };
        }

        public void UpdateProfile(string name, string? phoneNumber, string avatarUrl, DateOnly? dateOfBirth)
        {
            if (!string.IsNullOrWhiteSpace(name))
                Name = name;
            if (!string.IsNullOrWhiteSpace(phoneNumber))
                PhoneNumber = phoneNumber;
            if (!string.IsNullOrWhiteSpace(avatarUrl))
                AvatarUrl = avatarUrl;
            if(dateOfBirth.HasValue)
                DateOfBirth = dateOfBirth.Value;
            IsProfileCompleted = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ResetPassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new ArgumentException("New password hash cannot be null or empty.", nameof(newPasswordHash));
            PasswordHash = newPasswordHash;
            UpdatedAt = DateTime.UtcNow;
        }

   

    }
}


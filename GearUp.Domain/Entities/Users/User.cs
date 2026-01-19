using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Entities.Posts;
using GearUp.Domain.Entities.RealTime;
using GearUp.Domain.Enums;



namespace GearUp.Domain.Entities.Users
{
    public class User
    {
        public Guid Id { get; private set; } = default!;
        public string? Provider { get; private set; }
        public string? ProviderUserId { get; private set; }
        public string Username { get; private set; } = default!;
        public string Email { get; private set; } = default!;
        public string Name { get; private set; } = default!;
        public string PasswordHash { get; private set; } = default!;
        public UserRole Role { get; private set; }
        public DateOnly DateOfBirth { get; private set; }
        public string? PhoneNumber { get; private set; }
        public string AvatarUrl { get; private set; } = "https://i.pravatar.cc/300";
        public bool IsEmailVerified { get; private set; }
        public string? PendingEmail { get; private set; }
        public bool IsPendingEmailVerified { get; private set; }

        private readonly List<Post> _posts;
        private readonly List<CarRental> _ownedRentals;
        private readonly List<CarRental> _bookedRentals;
        private readonly List<Appointment> _receivedAppointments;
        private readonly List<Appointment> _sentAppointments;
        private readonly List<Notification> _notifications;
        private readonly List<Car> _cars;
        private readonly List<ConversationParticipant> _conversationParticipants;
        private readonly List<KycSubmissions> _kycSubmitted;
        private readonly List<KycSubmissions> _kycSubmissionsReviewed;
        private readonly List<UserPreference> _preferences;
        private readonly List<Notification> _notificationsTriggered;
        public IReadOnlyCollection<ConversationParticipant> ConversationParticipants => _conversationParticipants;
        public IReadOnlyCollection<Post> Posts => _posts;
        public IReadOnlyCollection<CarRental> OwnedRentals => _ownedRentals;
        public IReadOnlyCollection<CarRental> BookedRentals => _bookedRentals;
        public IReadOnlyCollection<Appointment> ReceivedAppointments => _receivedAppointments;
        public IReadOnlyCollection<Appointment> SentAppointments => _sentAppointments;
        public IReadOnlyCollection<Notification> Notifications => _notifications;
        public IReadOnlyCollection<Notification> NotificationsTriggered => _notificationsTriggered;
        public IReadOnlyCollection<Car> Cars => _cars;
        public IReadOnlyCollection<KycSubmissions> KycSubmitted => _kycSubmitted;
        public IReadOnlyCollection<KycSubmissions> KycSubmissionsReviewed => _kycSubmissionsReviewed;
        public IReadOnlyCollection<UserPreference> UserPreferences => _preferences;
        public bool IsDeleted { get; private set; } = false;
        public DateTime? DeletedAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private User()
        {
            Id = Guid.NewGuid();
            _posts = [];
            _bookedRentals = [];
            _ownedRentals = [];
            _receivedAppointments = [];
            _sentAppointments = [];
            _notifications = [];
            _cars = [];
            _conversationParticipants = [];
            _kycSubmitted = [];
            _kycSubmissionsReviewed = [];
            _preferences = [];
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public static User CreateLocalUser(string username, string email, string name, bool isEmailVerified = false, UserRole role = UserRole.Customer)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));

            return new User
            {
                Username = username.ToLower(),
                Email = email.ToLower(),
                Name = name,
                Role = role,
                AvatarUrl = "https://i.pravatar.cc/300",
                IsEmailVerified = isEmailVerified,
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
                Role = UserRole.Customer,
                AvatarUrl = "https://i.pravatar.cc/300",
                IsEmailVerified = false,

            };
        }

        public void UpdateProfile(
    string? name,
    string? phoneNumber,
    string? avatarUrl,
    DateOnly? dateOfBirth,
    string? newHashedPassword = null)
        {
            if (!string.IsNullOrWhiteSpace(name))
                Name = name.Trim();

            if (!string.IsNullOrWhiteSpace(phoneNumber))
                PhoneNumber = phoneNumber.Trim();

            if (!string.IsNullOrWhiteSpace(avatarUrl))
                AvatarUrl = avatarUrl.Trim();

            if (dateOfBirth.HasValue)
                DateOfBirth = dateOfBirth.Value;

            if (!string.IsNullOrEmpty(newHashedPassword))
                PasswordHash = newHashedPassword;

            UpdatedAt = DateTime.UtcNow;
        }


        public void SetPassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new ArgumentException("New password hash cannot be null or empty.", nameof(newPasswordHash));
            PasswordHash = newPasswordHash;
            UpdatedAt = DateTime.UtcNow;
        }

        public void VerifyEmail()
        {
            IsEmailVerified = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void VerifyPendingEmail()
        {
            if (string.IsNullOrEmpty(PendingEmail))
                return;

            Email = PendingEmail.ToLower();
            PendingEmail = null;
            IsPendingEmailVerified = true;
            UpdatedAt = DateTime.UtcNow;
        }


        public void SetPendingEmail(string newEmail)
        {
            PendingEmail = newEmail?.ToLower();
            IsPendingEmailVerified = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetIsPendingEmailVerified(bool isVerified)
        {
            IsPendingEmailVerified = isVerified;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetRole(UserRole newRole)
        {
            Role = newRole;
            UpdatedAt = DateTime.UtcNow;
        }


    }
}


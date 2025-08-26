using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Entities.Chats;
using GearUp.Domain.Entities.Posts;
using GearUp.Domain.Entities.RealTime;
using GearUp.Domain.Entities.Tokens;
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
        private readonly List<RefreshToken> _refreshTokens = new List<RefreshToken>();
        private readonly List<EmailVerificationToken> _emailVerificationTokens = new List<EmailVerificationToken>();
        private readonly List<PasswordResetToken> _passwordResetTokens = new List<PasswordResetToken>();
        private readonly List<Post> _posts = new List<Post>();
        private readonly List<PostComment> _comments = new List<PostComment>();
        private readonly List<PostLike> _likes = new List<PostLike>();
        private readonly List<PostView> _views = new List<PostView>();
        private readonly List<UserReview> _reviews = new List<UserReview>();
        private readonly List<CarRental> _ownedRentals = new List<CarRental>();
        private readonly List<CarRental> _bookedRentals = new List<CarRental>();
        private readonly List<Appointment> _receivedAppointments = new List<Appointment>();
        private readonly List<Appointment> _sentAppointments = new List<Appointment>();
        private readonly List<Conversation> _conversations = new List<Conversation>();
        private readonly List<Message> _sentMessages = new List<Message>();
        private readonly List<Message> _receivedMessages = new List<Message>();
        private readonly List<Notification> _notifications = new List<Notification>();

        public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
        public IReadOnlyCollection<EmailVerificationToken> EmailVerificationTokens => _emailVerificationTokens.AsReadOnly();
        public IReadOnlyCollection<PasswordResetToken> PasswordResetTokens => _passwordResetTokens.AsReadOnly();
        public IReadOnlyCollection<Post> Posts => _posts.AsReadOnly();
        public IReadOnlyCollection<PostComment> Comments => _comments.AsReadOnly();
        public IReadOnlyCollection<PostLike> Likes => _likes.AsReadOnly();
        public IReadOnlyCollection<PostView> Views => _views.AsReadOnly();
        public IReadOnlyCollection<UserReview> Reviews => _reviews.AsReadOnly();
        public IReadOnlyCollection<CarRental> OwnedRentals => _ownedRentals.AsReadOnly();
        public IReadOnlyCollection<CarRental> BookedRentals => _bookedRentals.AsReadOnly();
        public IReadOnlyCollection<Appointment> ReceivedAppointments => _receivedAppointments.AsReadOnly();
        public IReadOnlyCollection<Appointment> SentAppointments => _sentAppointments.AsReadOnly();
        public IReadOnlyCollection<Conversation> Conversations => _conversations.AsReadOnly();
        public IReadOnlyCollection<Message> SentMessages => _sentMessages.AsReadOnly();
        public IReadOnlyCollection<Message> ReceivedMessages => _receivedMessages.AsReadOnly();
        public IReadOnlyCollection<Notification> Notifications => _notifications.AsReadOnly();

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private User()
        {
            Id = Guid.NewGuid();
            _bookedRentals = new List<CarRental>();
            _ownedRentals = new List<CarRental>();
            _receivedAppointments = new List<Appointment>();
            _sentAppointments = new List<Appointment>();
            _conversations = new List<Conversation>();
            _sentMessages = new List<Message>();
            _receivedMessages = new List<Message>();
            _notifications = new List<Notification>();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;    
        }

        public static User CreateLocalUser(string username, string email, string name, string passwordHash, UserRole role)
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
                Role = role,
                IsProfileCompleted = false,
                IsEmailVerified = false,
              
            };
        }

        public static User CreateSocialUser(string provider, string providerUserId, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provider cannot be null or empty.", nameof(provider));
            if (string.IsNullOrWhiteSpace(providerUserId))
                throw new ArgumentException("Provider user ID cannot be null or empty.", nameof(providerUserId));
            return new User
            {
               
                Provider = provider,
                ProviderUserId = providerUserId,
                Role = role,
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


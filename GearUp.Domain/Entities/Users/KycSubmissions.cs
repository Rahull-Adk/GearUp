using GearUp.Domain.Entities.Users;
using System.ComponentModel;

namespace GearUp.Domain.Entities
{
    public class KycSubmissions
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid UserId { get; private set; }
        public Guid? ReviewerId { get; private set; }
        public KycStatus Status { get; private set; } 
        public KycDocumentType DocumentType { get; private set; } = KycDocumentType.Default;
        public string SelfieUrl { get; private set; } = string.Empty;
        public DateTime SubmittedAt { get; private set; }
        public DateTime? VerifiedAt { get; private set; }
        public string? RejectionReason { get; private set; }
        public List<Uri> DocumentUrls { get; private set; } = new List<Uri>();


        public User SubmittedBy { get; private set; } = default!;
        public User? ReviewedBy { get; private set; }

        private KycSubmissions()
        {

        }

        public static KycSubmissions CreateKycSubmissions(Guid userId, KycDocumentType documentType,List<Uri> DocumentUrls,  string selfieUrl) {
            if(userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty", nameof(userId));
            if(documentType == KycDocumentType.Default)
                throw new ArgumentException("DocumentType cannot be default", nameof(documentType));
            if(string.IsNullOrWhiteSpace(selfieUrl))
                throw new ArgumentException("SelfieUrl cannot be null or empty", nameof(selfieUrl));
            if(DocumentUrls == null || DocumentUrls.Count == 0)
                throw new ArgumentException("DocumentUrls cannot be null or empty", nameof(DocumentUrls));

            return new KycSubmissions
            {
                UserId = userId,
                DocumentType = documentType,
                DocumentUrls = DocumentUrls,
                SelfieUrl = selfieUrl,
                Status = KycStatus.Pending,
                SubmittedAt = DateTime.UtcNow

            };
        }


    }


    public enum KycStatus
    {
        Pending = 1,
        Approved = 2,
        rejected = 3
    }
    public enum KycDocumentType
    {
        [Description("Default")]
        Default = 0,
        [Description("Passport")]
        Passport = 1,
        [Description("National ID")]
        NationalID = 2,
        [Description("Driver License")]
        DriverLicense = 3,
        [Description("Utility Bill")]
        UtilityBill = 4,
        [Description("Other")]
        Other = 5

    }
}

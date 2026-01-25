using System.Runtime.CompilerServices;
using Bogus;
using GearUp.Domain.Entities;
using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Entities.Posts;
using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;
using GearUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Seed;

public class DbSeeder
{
    private readonly GearUpDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public DbSeeder(GearUpDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync()
    {
        await SeedFakeUsersAsync(200);
        await SeedFakeCarsAsync(1000);
        await SeedFakePostsAsync(300);
        await SeedFakeCommentsAsync(1800);
        await SeedFakeNestedCommentAsync(5000);
        await SeedFakePostLikeAsync(2000);
        await SeedFakeKycAsync(30);
        await SeedFakeAppointmentAsync(40);
       await SeedFakeReviewAsync(20);
    }

    private async Task SeedFakeUsersAsync(int targetCount)
    {
        int existing = await _context.Users
            .CountAsync(u => u.Email.EndsWith("@example.com"));

        if (existing >= targetCount)
            return;

        int toCreate = targetCount - existing;

        var faker = new Faker("en");

        var newUsers = new List<User>(toCreate);

        for (int i = 0; i < toCreate; i++)
        {
            UserRole role = i switch
            {
                <= 50 => UserRole.Dealer,
                <= 100 => UserRole.Renter,
                _ => UserRole.Customer
            };

            var username = faker.Internet.UserName().ToLower();
            var email = $"{username}@example.com";
            var name = faker.Name.FullName();

            // Use your DDD factory
            var user = User.CreateLocalUser(username, email, name, true, role);

            // Hash password using IPasswordHasher<User>
            var hashed = _passwordHasher.HashPassword(user, "Password123!");
            // Use your domain method to set the password; you previously used SetPassword
            user.SetPassword(hashed);

            newUsers.Add(user);
        }

        await _context.Users.AddRangeAsync(newUsers);
        await _context.SaveChangesAsync();
    }

    private async Task SeedFakeCarsAsync(int targetCount)
    {
        int existing = await _context.Cars
            .CountAsync(c => c.Title.EndsWith("@example.com"));

        if (existing >= targetCount)
            return;

        int toCreate = targetCount - existing;

        var faker = new Faker("en");
        var userIds = await _context.Users
            .Where(u => u.Email.EndsWith("@example.com") && u.Role == UserRole.Dealer)
            .Select(u => u.Id)
            .ToListAsync();

        if (userIds.Count == 0)
        {
            throw new InvalidOperationException(
                "No fake users found. Ensure SeedFakeUsersAsync runs before seeding cars.");
        }

        var newCars = new List<Car>(toCreate);
        var newCarImages = new List<CarImage>();

        for (int i = 0; i < toCreate; i++)
        {
            var title = faker.Vehicle.Model();
            var description = faker.Lorem.Paragraph();
            var model = faker.Vehicle.Model();
            var make = faker.Vehicle.Manufacturer();
            var year = faker.Date.Past(10).Year;
            var price = faker.Random.Double(1000, 100000);
            var color = faker.Commerce.Color();
            var mileage = faker.Random.Int(1000, 200000);
            var seatingCapacity = faker.Random.Int(2, 8);
            var engineCapacity = faker.Random.Int(800, 5000);
            var fuelType = faker.PickRandom<FuelType>();
            var condition = faker.PickRandom<CarCondition>();
            var transmission = faker.PickRandom<TransmissionType>();
            var dealerId = faker.PickRandom(userIds);
            var vin = faker.Vehicle.Vin();
            var licensePlate = Random.Shared.Next(1000, 9999).ToString();
            var validationStatus = faker.Random.WeightedRandom<CarValidationStatus>(
                [CarValidationStatus.Pending, CarValidationStatus.Approved, CarValidationStatus.Rejected],
                [0.07f, 0.9f, 0.03f]
            );
            var carStatus = faker.Random.WeightedRandom<CarStatus>(
                [CarStatus.Available, CarStatus.Sold, CarStatus.Reserved],
                [0.9f, 0.09f, 0.01f]
            );

            var car = Car.CreateForSale(
                Id: Guid.NewGuid(),
                title: title,
                description: description,
                model: model,
                make: make,
                year: year,
                price: price,
                color: color,
                mileage: mileage,
                seatingCapacity: seatingCapacity,
                engineCapacity: engineCapacity,
                imageUrls: null,
                fuelType: fuelType,
                condition: condition,
                transmission: transmission,
                dealerId: dealerId,
                vin: vin,
                licensePlate: licensePlate,
                validationStatus: validationStatus,
                status: carStatus
            );

            newCars.Add(car);
            int imgCount = faker.Random.Int(3, 6);

            for (int j = 0; j < imgCount; j++)
            {
                var image = CarImage.CreateCarImage(
                    carId: car.Id,
                    url: faker.Image.PicsumUrl()
                );

                newCarImages.Add(image);
            }
        }

        await _context.Cars.AddRangeAsync(newCars);
        await _context.CarImages.AddRangeAsync(newCarImages);
        await _context.SaveChangesAsync();
    }

    private async Task SeedFakePostsAsync(int targetCount)
    {
        int existing = await _context.Posts
            .CountAsync(p => p.Content.EndsWith("@example.com"));

        if (existing >= targetCount)
            return;

        int toCreate = targetCount - existing;

        var faker = new Faker("en");

        var userIds = await _context.Users
            .Where(u => u.Email.EndsWith("@example.com") && u.Role == UserRole.Dealer)
            .Select(u => u.Id)
            .ToListAsync();

        var carIds = await _context.Cars
            .Select(c => c.Id)
            .ToListAsync();

        var newPosts = new List<Post>(toCreate);

        for (int i = 0; i < toCreate; i++)
        {
            var post = Post.CreatePost(
                caption: faker.Lorem.Sentence(),
                content: $"{faker.Lorem.Paragraph()}@example.com",
                visibility: faker.PickRandom<PostVisibility>(),
                userId: faker.PickRandom(userIds),
                carId: faker.PickRandom(carIds)
            );

            newPosts.Add(post);
        }

        await _context.Posts.AddRangeAsync(newPosts);
        await _context.SaveChangesAsync();
    }

    private async Task SeedFakeCommentsAsync(int targetCount)
    {
        int existing = await _context.PostComments
            .CountAsync(c => c.Content.EndsWith("seeded_comment"));
        if (existing >= targetCount)
            return;
        int toCreate = targetCount - existing;
        var faker = new Faker("en");
        var userIds = await _context.Users
            .Where(u => u.Email.EndsWith("@example.com"))
            .Select(u => u.Id)
            .ToListAsync();
        var postIds = await _context.Posts
            .Select(p => p.Id)
            .ToListAsync();
        var newComments = new List<PostComment>(toCreate);
        for (int i = 0; i < toCreate; i++)
        {
            var comment = PostComment.CreateComment(
                content: $"{faker.Lorem.Sentence()}seeded_comment",
                postId: faker.PickRandom(postIds),
                commentedUserId: faker.PickRandom(userIds)
            );
            newComments.Add(comment);
        }

        await _context.PostComments.AddRangeAsync(newComments);
        await _context.SaveChangesAsync();
    }

    private async Task SeedFakeNestedCommentAsync(int targetCount)
    {
        int existing = await _context.PostLikes
            .CountAsync();
        if (existing >= targetCount)
            return;
        int toCreate = targetCount - existing;
        var faker = new Faker("en");
        var userIds = await _context.Users
            .Where(u => u.Email.EndsWith("@example.com"))
            .Select(u => u.Id)
            .ToListAsync();
        var commentIds = await _context.PostComments
            .Select(p => p.Id)
            .ToListAsync();
        var parentComments = await _context.PostComments
            .Select(c => new { c.Id, c.PostId })
            .ToListAsync();

        var nestedComments = new List<PostComment>(toCreate);
        for (int i = 0; i < toCreate; i++)
        {
            var parent = faker.PickRandom(parentComments);
            var comment = PostComment.CreateComment(
                content: $"{faker.Lorem.Sentence()}seeded_nested_comment",
                postId: parent.PostId,
                commentedUserId: faker.PickRandom(userIds),
                parentCommentId: faker.PickRandom(commentIds)
            );
            nestedComments.Add(comment);
        }

        await _context.PostComments.AddRangeAsync(nestedComments);
        await _context.SaveChangesAsync();
    }

    private async Task SeedFakePostLikeAsync(int targetCount)
    {
        int existing = await _context.PostLikes
            .CountAsync();
        if (existing >= targetCount)
            return;
        int toCreate = targetCount - existing;
        var faker = new Faker("en");
        var userIds = await _context.Users
            .Where(u => u.Email.EndsWith("@example.com"))
            .Select(u => u.Id)
            .ToListAsync();
        var postIds = await _context.Posts
            .Select(p => p.Id)
            .ToListAsync();
        var newLikes = new List<PostLike>(toCreate);
        for (int i = 0; i < toCreate; i++)
        {
            var like = PostLike.CreateLike(
                postId: faker.PickRandom(postIds),
                likedUserId: faker.PickRandom(userIds)
            );
            newLikes.Add(like);
        }

        await _context.PostLikes.AddRangeAsync(newLikes);
        await _context.SaveChangesAsync();
    }

    private async Task SeedFakeKycAsync(int targetCount)
    {
        var existing = await _context.KycSubmissions.CountAsync();
        if (existing >= targetCount)
            return;
        int toCreate = targetCount - existing;
        var faker = new Faker("en");

        var userId = await _context.Users
            .Where(u => u.Email.EndsWith("@example.com") && u.Role == UserRole.Customer)
            .Select(u => u.Id).ToListAsync();

        var kycSubmissions = new List<KycSubmissions>(toCreate);
        for (int i = 0; i < toCreate; i++)
        {
            var kycSubmission = KycSubmissions.CreateKycSubmissions(faker.PickRandom(userId),
                faker.Random.WeightedRandom([KycDocumentType.Default, KycDocumentType.DriverLicense, KycDocumentType.NationalID, KycDocumentType.Passport, KycDocumentType.Other, KycDocumentType.UtilityBill], [0.0f, 0.2f, 0.2f, 0.2f,0.1f, 0.2f]),
                [
                    new Uri(faker.Image.PicsumUrl()),
                    new Uri(faker.Image.PicsumUrl())
                ],
                faker.Image.PicsumUrl(), faker.PickRandom<KycStatus>());
            kycSubmissions.Add(kycSubmission);
        }

        await _context.KycSubmissions.AddRangeAsync(kycSubmissions);
        await _context.SaveChangesAsync();
    }

    private async Task SeedFakeAppointmentAsync(int targetCount)
    {
        int existing = await _context.Appointments.CountAsync();
        if (existing >= targetCount)
            return;
        int toCreate = targetCount - existing;
        var faker = new Faker("en");
        var appointments = new List<Appointment>(toCreate);
        var agentIds = await _context.Users.Where(u => u.Email.EndsWith("@example.com") && u.Role == UserRole.Dealer).Select(u => u.Id).ToListAsync();
        var requesterIds = await _context.Users.Where(u => u.Email.EndsWith("@example.com") && u.Role == UserRole.Customer).Select(u => u.Id).ToListAsync();
        for (int i = 0; i < toCreate; i++)
        {
            var appointment = Appointment.CreateAppointment(faker.PickRandom(agentIds), faker.PickRandom(requesterIds), faker.Date.Soon(), faker.Lorem.Slug(6), faker.Lorem.Sentence(), faker.Random.WeightedRandom([AppointmentStatus.Completed, AppointmentStatus.Cancelled,  AppointmentStatus.Pending, AppointmentStatus.Rejected, AppointmentStatus.Scheduled], [0.2f, 0.2f,0.2f,0.2f,0.2f]), null);

            appointments.Add(appointment);
        }

        await _context.Appointments.AddRangeAsync(appointments);
        await _context.SaveChangesAsync();
    }

   private async Task SeedFakeReviewAsync(int targetCount)
    {
        var existing = await _context.UserReviews.CountAsync();
        if (existing >= targetCount)
            return;
        int toCreate = targetCount - existing;
        var faker = new Faker("en");

        // Get existing reviewer-dealer pairs that already have reviews
        var existingReviewPairs = await _context.UserReviews
            .Select(r => new { r.ReviewerId, r.RevieweeId })
            .ToListAsync();

        // Get unique customer-dealer pairs from completed appointments that don't have reviews yet
        var availablePairs = await _context.Appointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .Select(a => new { CustomerId = a.RequesterId, DealerId = a.AgentId })
            .Distinct()
            .ToListAsync();

        // Filter out pairs that already have reviews
        var pairsToReview = availablePairs
            .Where(p => !existingReviewPairs.Any(e => e.ReviewerId == p.CustomerId && e.RevieweeId == p.DealerId))
            .ToList();

        if (pairsToReview.Count == 0)
            return;

        // Limit toCreate to available pairs (one review per dealer per customer)
        toCreate = Math.Min(toCreate, pairsToReview.Count);

        var reviews = new List<UserReview>(toCreate);
        for (int i = 0; i < toCreate; i++)
        {
            var pair = pairsToReview[i];

            var rating = Math.Round(faker.Random.Double(1.0, 5.0) * 2) / 2;
            var review = UserReview.Create(
                pair.CustomerId,
                pair.DealerId,
                faker.Lorem.Text(),
                rating);

            reviews.Add(review);
        }

        await _context.UserReviews.AddRangeAsync(reviews);
        await _context.SaveChangesAsync();

    }


}
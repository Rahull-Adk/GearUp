using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.AppointmentServiceInterface;
using GearUp.Application.ServiceDtos.Appointment;
using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Appointments
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICarRepository _carRepository;
        private readonly ICommonRepository _commonRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IUserRepository userRepository,
            ICarRepository carRepository,
            ICommonRepository commonRepository,
            INotificationService notificationService,
            ILogger<AppointmentService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _userRepository = userRepository;
            _carRepository = carRepository;
            _commonRepository = commonRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Result<AppointmentResponseDto>> CreateAppointmentAsync(CreateAppointmentRequestDto dto, Guid requesterId)
        {
            _logger.LogInformation("Creating appointment for requester {RequesterId} with dealer {DealerId}", requesterId, dto.AgentId);

            // Validate dealer exists
            var dealer = await _userRepository.GetUserByIdAsync(dto.AgentId);
            if (dealer == null)
            {
                _logger.LogWarning("Dealer not found with ID: {DealerId}", dto.AgentId);
                return Result<AppointmentResponseDto>.Failure("Dealer not found.", 404);
            }

            // Validate requester exists
            var requester = await _userRepository.GetUserByIdAsync(requesterId);
            if (requester == null)
            {
                _logger.LogWarning("Requester not found with ID: {RequesterId}", requesterId);
                return Result<AppointmentResponseDto>.Failure("Requester not found.", 404);
            }

            // Validate car if provided
            string? carTitle = null;
            if (dto.CarId.HasValue)
            {
                var car = await _carRepository.GetCarByIdAsync(dto.CarId.Value);
                if (car == null)
                {
                    _logger.LogWarning("Car not found with ID: {CarId}", dto.CarId);
                    return Result<AppointmentResponseDto>.Failure("Car not found.", 404);
                }
                carTitle = car.Title;
            }

            // Validate schedule is in the future
            if (dto.Schedule <= DateTime.UtcNow)
            {
                return Result<AppointmentResponseDto>.Failure("Appointment schedule must be in the future.", 400);
            }

            var appointment = Appointment.CreateAppointment(
                dto.AgentId,
                requesterId,
                dto.Schedule,
                dto.Location,
                dto.Notes,
                status: AppointmentStatus.Pending,
                dto.CarId
            );

            await _appointmentRepository.AddAsync(appointment);
            await _commonRepository.SaveChangesAsync();

            // Create and push notification to dealer
            await _notificationService.CreateAndPushNotificationAsync(
                "New appointment request",
                $"{requester.Name} requested an appointment with you.",
                NotificationEnum.AppointmentRequested,
                actorUserId: requesterId,
                receiverUserId: dto.AgentId,
                appointmentId: appointment.Id
            );


            var responseDto = new AppointmentResponseDto
            {
                Id = appointment.Id,
                AgentId = appointment.AgentId,
                AgentName = dealer.Name,
                RequesterId = appointment.RequesterId,
                RequesterName = requester.Name,
                CarId = appointment.CarId,
                CarTitle = carTitle,
                Schedule = appointment.Schedule,
                Location = appointment.Location,
                Status = appointment.Status,
                Notes = appointment.Notes,
                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt
            };

            _logger.LogInformation("Appointment {AppointmentId} created successfully", appointment.Id);
            return Result<AppointmentResponseDto>.Success(responseDto, "Appointment created successfully.", 201);
        }

        public async Task<Result<AppointmentResponseDto>> GetAppointmentByIdAsync(Guid appointmentId, Guid userId)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return Result<AppointmentResponseDto>.Failure("Appointment not found.", 404);
            }

            // Only allow dealer or requester to view
            if (appointment.AgentId != userId && appointment.RequesterId != userId)
            {
                return Result<AppointmentResponseDto>.Failure("You don't have permission to view this appointment.", 403);
            }

            var dealer = await _userRepository.GetUserByIdAsync(appointment.AgentId);
            var requester = await _userRepository.GetUserByIdAsync(appointment.RequesterId);

            string? carTitle = null;
            if (appointment.CarId.HasValue)
            {
                var car = await _carRepository.GetCarByIdAsync(appointment.CarId.Value);
                carTitle = car?.Title;
            }

            var responseDto = new AppointmentResponseDto
            {
                Id = appointment.Id,
                AgentId = appointment.AgentId,
                AgentName = dealer?.Name ?? "Unknown",
                RequesterId = appointment.RequesterId,
                RequesterName = requester?.Name ?? "Unknown",
                CarId = appointment.CarId,
                CarTitle = carTitle,
                Schedule = appointment.Schedule,
                Location = appointment.Location,
                Status = appointment.Status,
                Notes = appointment.Notes,
                RejectionReason = appointment.RejectionReason,
                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt
            };

            return Result<AppointmentResponseDto>.Success(responseDto, "Appointment retrieved successfully.", 200);
        }

        public async Task<Result<CursorPageResult<AppointmentResponseDto>>> GetDealerAppointmentsAsync(Guid dealerId, string? cursorString)
        {
            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<AppointmentResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var appointments = await _appointmentRepository.GetByDealerIdAsync(dealerId, cursor);
            return Result<CursorPageResult<AppointmentResponseDto>>.Success(appointments, "Appointments retrieved successfully.", 200);
        }

        public async Task<Result<CursorPageResult<AppointmentResponseDto>>> GetCustomerAppointmentsAsync(Guid customerId, string? cursorString)
        {
            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<AppointmentResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var appointments = await _appointmentRepository.GetByRequesterIdAsync(customerId, cursor);
            return Result<CursorPageResult<AppointmentResponseDto>>.Success(appointments, "Appointments retrieved successfully.", 200);
        }

        public async Task<Result<AppointmentResponseDto>> AcceptAppointmentAsync(Guid appointmentId, Guid dealerId)
        {
            _logger.LogInformation("Dealer {DealerId} accepting appointment {AppointmentId}", dealerId, appointmentId);

            var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return Result<AppointmentResponseDto>.Failure("Appointment not found.", 404);
            }

            if (appointment.AgentId != dealerId)
            {
                return Result<AppointmentResponseDto>.Failure("You don't have permission to accept this appointment.", 403);
            }

            try
            {
                appointment.AcceptAppointment();
            }
            catch (InvalidOperationException ex)
            {
                return Result<AppointmentResponseDto>.Failure(ex.Message, 400);
            }

            var dealer = await _userRepository.GetUserByIdAsync(dealerId);
            var requester = await _userRepository.GetUserByIdAsync(appointment.RequesterId);

            await _commonRepository.SaveChangesAsync();

            // Create and push notification to requester
            await _notificationService.CreateAndPushNotificationAsync(
                "Appointment update",
                $"{dealer?.Name ?? "Dealer"} accepted your appointment request.",
                NotificationEnum.AppointmentAccepted,
                actorUserId: dealerId,
                receiverUserId: appointment.RequesterId,
                appointmentId: appointmentId
            );


            string? carTitle = null;
            if (appointment.CarId.HasValue)
            {
                var car = await _carRepository.GetCarByIdAsync(appointment.CarId.Value);
                carTitle = car?.Title;
            }

            var responseDto = new AppointmentResponseDto
            {
                Id = appointment.Id,
                AgentId = appointment.AgentId,
                AgentName = dealer?.Name ?? "Unknown",
                RequesterId = appointment.RequesterId,
                RequesterName = requester?.Name ?? "Unknown",
                CarId = appointment.CarId,
                CarTitle = carTitle,
                Schedule = appointment.Schedule,
                Location = appointment.Location,
                Status = appointment.Status,
                Notes = appointment.Notes,
                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt
            };

            _logger.LogInformation("Appointment {AppointmentId} accepted successfully", appointmentId);
            return Result<AppointmentResponseDto>.Success(responseDto, "Appointment accepted successfully.", 200);
        }

        public async Task<Result<AppointmentResponseDto>> RejectAppointmentAsync(Guid appointmentId, Guid dealerId, string? reason)
        {
            _logger.LogInformation("Dealer {DealerId} rejecting appointment {AppointmentId}", dealerId, appointmentId);

            var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return Result<AppointmentResponseDto>.Failure("Appointment not found.", 404);
            }

            if (appointment.AgentId != dealerId)
            {
                return Result<AppointmentResponseDto>.Failure("You don't have permission to reject this appointment.", 403);
            }

            try
            {
                appointment.RejectAppointment(reason);
            }
            catch (InvalidOperationException ex)
            {
                return Result<AppointmentResponseDto>.Failure(ex.Message, 400);
            }

            var dealer = await _userRepository.GetUserByIdAsync(dealerId);
            var requester = await _userRepository.GetUserByIdAsync(appointment.RequesterId);

            await _commonRepository.SaveChangesAsync();

            // Create and push notification to requester
            await _notificationService.CreateAndPushNotificationAsync(
                "Appointment update",
                $"{dealer?.Name ?? "Dealer"} rejected your appointment request.",
                NotificationEnum.AppointmentRejected,
                actorUserId: dealerId,
                receiverUserId: appointment.RequesterId,
                appointmentId: appointmentId
            );


            string? carTitle = null;
            if (appointment.CarId.HasValue)
            {
                var car = await _carRepository.GetCarByIdAsync(appointment.CarId.Value);
                carTitle = car?.Title;
            }

            var responseDto = new AppointmentResponseDto
            {
                Id = appointment.Id,
                AgentId = appointment.AgentId,
                AgentName = dealer?.Name ?? "Unknown",
                RequesterId = appointment.RequesterId,
                RequesterName = requester?.Name ?? "Unknown",
                CarId = appointment.CarId,
                CarTitle = carTitle,
                Schedule = appointment.Schedule,
                Location = appointment.Location,
                Status = appointment.Status,
                Notes = appointment.Notes,
                RejectionReason = appointment.RejectionReason,
                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt
            };

            _logger.LogInformation("Appointment {AppointmentId} rejected successfully", appointmentId);
            return Result<AppointmentResponseDto>.Success(responseDto, "Appointment rejected successfully.", 200);
        }

        public async Task<Result<AppointmentResponseDto>> CancelAppointmentAsync(Guid appointmentId, Guid userId)
        {
            _logger.LogInformation("User {UserId} cancelling appointment {AppointmentId}", userId, appointmentId);

            var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return Result<AppointmentResponseDto>.Failure("Appointment not found.", 404);
            }

            // Only requester can cancel their own appointment
            if (appointment.RequesterId != userId)
            {
                return Result<AppointmentResponseDto>.Failure("You don't have permission to cancel this appointment.", 403);
            }

            try
            {
                appointment.CancelAppointment();
            }
            catch (InvalidOperationException ex)
            {
                return Result<AppointmentResponseDto>.Failure(ex.Message, 400);
            }

            var dealer = await _userRepository.GetUserByIdAsync(appointment.AgentId);
            var requester = await _userRepository.GetUserByIdAsync(userId);

            await _commonRepository.SaveChangesAsync();

            string? carTitle = null;
            if (appointment.CarId.HasValue)
            {
                var car = await _carRepository.GetCarByIdAsync(appointment.CarId.Value);
                carTitle = car?.Title;
            }

            var responseDto = new AppointmentResponseDto
            {
                Id = appointment.Id,
                AgentId = appointment.AgentId,
                AgentName = dealer?.Name ?? "Unknown",
                RequesterId = appointment.RequesterId,
                RequesterName = requester?.Name ?? "Unknown",
                CarId = appointment.CarId,
                CarTitle = carTitle,
                Schedule = appointment.Schedule,
                Location = appointment.Location,
                Status = appointment.Status,
                Notes = appointment.Notes,
                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt
            };

            _logger.LogInformation("Appointment {AppointmentId} cancelled successfully", appointmentId);
            return Result<AppointmentResponseDto>.Success(responseDto, "Appointment cancelled successfully.", 200);
        }

        public async Task<Result<AppointmentResponseDto>> CompleteAppointmentAsync(Guid appointmentId, Guid dealerId)
        {
            _logger.LogInformation("Dealer {DealerId} completing appointment {AppointmentId}", dealerId, appointmentId);

            var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return Result<AppointmentResponseDto>.Failure("Appointment not found.", 404);
            }

            if (appointment.AgentId != dealerId)
            {
                return Result<AppointmentResponseDto>.Failure("You don't have permission to complete this appointment.", 403);
            }

            try
            {
                appointment.CompleteAppointment();
            }
            catch (InvalidOperationException ex)
            {
                return Result<AppointmentResponseDto>.Failure(ex.Message, 400);
            }

            var dealer = await _userRepository.GetUserByIdAsync(dealerId);
            var requester = await _userRepository.GetUserByIdAsync(appointment.RequesterId);

            await _commonRepository.SaveChangesAsync();

            string? carTitle = null;
            if (appointment.CarId.HasValue)
            {
                var car = await _carRepository.GetCarByIdAsync(appointment.CarId.Value);
                carTitle = car?.Title;
            }

            var responseDto = new AppointmentResponseDto
            {
                Id = appointment.Id,
                AgentId = appointment.AgentId,
                AgentName = dealer?.Name ?? "Unknown",
                RequesterId = appointment.RequesterId,
                RequesterName = requester?.Name ?? "Unknown",
                CarId = appointment.CarId,
                CarTitle = carTitle,
                Schedule = appointment.Schedule,
                Location = appointment.Location,
                Status = appointment.Status,
                Notes = appointment.Notes,
                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt
            };

            _logger.LogInformation("Appointment {AppointmentId} completed successfully", appointmentId);
            return Result<AppointmentResponseDto>.Success(responseDto, "Appointment completed successfully.");
        }
    }
}

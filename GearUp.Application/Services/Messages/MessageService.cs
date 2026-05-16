using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces.Messaging;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.MessageServiceInterface;
using GearUp.Application.Messaging.Contracts;
using GearUp.Application.ServiceDtos.Message;
using GearUp.Domain.Entities.RealTime;
using GearUp.Domain.Enums;
using GearUp.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Messages
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICommonRepository _commonRepository;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            IMessageRepository messageRepository,
            IUserRepository userRepository,
            ICommonRepository commonRepository,
            IMessagePublisher messagePublisher,
            ILogger<MessageService> logger)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _commonRepository = commonRepository;
            _messagePublisher = messagePublisher;
            _logger = logger;
        }

        public async Task<Result<MessageResponseDto>> SendMessageAsync(SendMessageRequestDto dto, Guid senderId)
        {
            _logger.LogInformation("User {SenderId} sending message to {ReceiverId}", senderId, dto.ReceiverId);

            if (string.IsNullOrWhiteSpace(dto.Text) && string.IsNullOrWhiteSpace(dto.ImageUrl))
            {
                throw new Domain.Exceptions.ValidationException("Message must contain text or image.");
            }

            if (senderId == dto.ReceiverId)
            {
                throw new Domain.Exceptions.ValidationException("You cannot send messages to yourself.");
            }

            var sender = await _userRepository.GetUserByIdAsync(senderId)
                         ?? throw new NotFoundException("Sender", senderId);

            var receiver = await _userRepository.GetUserByIdAsync(dto.ReceiverId)
                           ?? throw new NotFoundException("Receiver", dto.ReceiverId);

            var isValidConversation =
                (sender.Role == UserRole.Customer && receiver.Role == UserRole.Dealer) ||
                (sender.Role == UserRole.Dealer && receiver.Role == UserRole.Customer);

            if (!isValidConversation)
            {
                throw new Domain.Exceptions.ValidationException("Messages can only be sent between customers and dealers.");
            }

            var conversation = await _messageRepository.GetConversationByParticipantsAsync(senderId, dto.ReceiverId);

            if (conversation == null)
            {
                conversation = Conversation.Create();
                conversation.AddParticipant(senderId);
                conversation.AddParticipant(dto.ReceiverId);
                await _messageRepository.AddConversationAsync(conversation);
            }

            // Create message
            var message = Message.Create(conversation.Id, senderId, dto.Text, dto.ImageUrl);
            await _messageRepository.AddMessageAsync(message);

            // Update conversation last message time
            conversation.TouchLastMessage(message.Id, message.SentAt);

            await _commonRepository.SaveChangesAsync();

            var responseDto = new MessageResponseDto
            {
                Id = message.Id,
                ConversationId = conversation.Id,
                SenderId = senderId,
                SenderName = sender.Name,
                SenderAvatarUrl = sender.AvatarUrl,
                Text = message.Text,
                ImageUrl = message.ImageUrl,
                SentAt = message.SentAt,
                EditedAt = message.EditedAt,
                IsMine = true
            };

            // Send real-time notification to receiver
            var receiverDto = new MessageResponseDto
            {
                Id = message.Id,
                ConversationId = conversation.Id,
                SenderId = senderId,
                SenderName = sender.Name,
                SenderAvatarUrl = sender.AvatarUrl,
                Text = message.Text,
                ImageUrl = message.ImageUrl,
                SentAt = message.SentAt,
                EditedAt = message.EditedAt,
                IsMine = false
            };


            var userMessage = new NotificationRequestMessage
            {
                MethodName = "SendMessageToUser",
                CorrelationId = dto.ReceiverId.ToString(),
                Payload = new Dictionary<string, object>
                {
                    ["receiverId"] = dto.ReceiverId,
                    ["message"] = receiverDto
                }
            };
            await _messagePublisher.PublishAsync(userMessage, "gearup.notification.queue");

            var conversationMessage = new NotificationRequestMessage
            {
                MethodName = "SendMessageToConversation",
                CorrelationId = conversation.Id.ToString(),
                Payload = new Dictionary<string, object>
                {
                    ["conversationId"] = conversation.Id,
                    ["excludeUserId"] = senderId,
                    ["message"] = receiverDto
                }
            };
            await _messagePublisher.PublishAsync(conversationMessage, "gearup.notification.queue");

            _logger.LogInformation("Message {MessageId} sent successfully from {SenderId} to {ReceiverId} and queued for real-time delivery",
                message.Id, senderId, dto.ReceiverId);

            return Result<MessageResponseDto>.Success(responseDto, "Message sent successfully.", 201);
        }

        public async Task<Result<CursorPageResult<ConversationResponseDto>>> GetConversationsAsync(Guid userId, string? cursorString, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting conversations for user {UserId}", userId);

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    throw new Domain.Exceptions.ValidationException("Invalid cursor");
                }
            }

            var conversations = await _messageRepository.GetUserConversationsAsync(userId, cursor, cancellationToken);

            return Result<CursorPageResult<ConversationResponseDto>>.Success(conversations, "Conversations retrieved successfully.");
        }

        public async Task<Result<ConversationDetailResponseDto>> GetConversationAsync(Guid conversationId, Guid userId, string? cursorString, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting conversation {ConversationId} for user {UserId}", conversationId, userId);

            var conversation = await _messageRepository.GetConversationByIdAsync(conversationId, cancellationToken)
                               ?? throw new NotFoundException("Conversation", conversationId);

            if (!await _messageRepository.IsParticipantInConversationAsync(conversationId, userId, cancellationToken))
            {
                throw new ForbiddenException("You are not a participant in this conversation.");
            }

            var otherParticipant = conversation.Participants.FirstOrDefault(p => p.UserId != userId);
            if (otherParticipant?.User == null)
            {
                throw new NotFoundException("Other participant not found.");
            }

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    throw new Domain.Exceptions.ValidationException("Invalid cursor");
                }
            }

            var messagesResult = await _messageRepository.GetConversationMessagesAsync(conversationId, cursor, cancellationToken);

            // Mark messages as read
            await _messageRepository.MarkMessagesAsReadAsync(conversationId, userId);
            await _commonRepository.SaveChangesAsync();

            var messageResponses = messagesResult.Items.Select(m => new MessageResponseDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderName = m.Sender?.Name ?? "Unknown",
                SenderAvatarUrl = m.Sender?.AvatarUrl,
                Text = m.Text,
                ImageUrl = m.ImageUrl,
                SentAt = m.SentAt,
                EditedAt = m.EditedAt,
                IsMine = m.SenderId == userId
            }).Reverse().ToList();

            var response = new ConversationDetailResponseDto
            {
                ConversationId = conversationId,
                OtherUserId = otherParticipant.UserId,
                OtherUserName = otherParticipant.User.Name,
                OtherUserAvatarUrl = otherParticipant.User.AvatarUrl,
                Messages = messageResponses,
                NextCursor = messagesResult.NextCursor,
                HasMore = messagesResult.HasMore
            };

            return Result<ConversationDetailResponseDto>.Success(response, "Conversation retrieved successfully.");
        }

        public async Task<Result<ConversationDetailResponseDto>> GetOrCreateConversationWithUserAsync(Guid currentUserId, Guid otherUserId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting or creating conversation between {CurrentUserId} and {OtherUserId}", currentUserId, otherUserId);

            if (currentUserId == otherUserId)
            {
                throw new Domain.Exceptions.ValidationException("Cannot create conversation with yourself.");
            }

            var currentUser = await _userRepository.GetUserByIdAsync(currentUserId)
                              ?? throw new NotFoundException("Current user not found.");

            var otherUser = await _userRepository.GetUserByIdAsync(otherUserId)
                            ?? throw new NotFoundException("Other user not found.");

            // Check if conversation between customer and dealer is valid
            var isValidConversation =
                (currentUser.Role == UserRole.Customer && otherUser.Role == UserRole.Dealer) ||
                (currentUser.Role == UserRole.Dealer && otherUser.Role == UserRole.Customer);

            if (!isValidConversation)
            {
                throw new Domain.Exceptions.ValidationException("Conversations can only be between customers and dealers.");
            }

            var conversation = await _messageRepository.GetConversationByParticipantsAsync(currentUserId, otherUserId, cancellationToken);

            if (conversation == null)
            {
                conversation = Conversation.Create();
                conversation.AddParticipant(currentUserId);
                conversation.AddParticipant(otherUserId);
                await _messageRepository.AddConversationAsync(conversation);
                await _commonRepository.SaveChangesAsync();

                // Reload to get participants with user info
                conversation = await _messageRepository.GetConversationByIdAsync(conversation.Id, cancellationToken);
            }

            var response = new ConversationDetailResponseDto
            {
                ConversationId = conversation!.Id,
                OtherUserId = otherUserId,
                OtherUserName = otherUser.Name,
                OtherUserAvatarUrl = otherUser.AvatarUrl,
                Messages = []
            };

            return Result<ConversationDetailResponseDto>.Success(response, "Conversation retrieved successfully.");
        }

        public async Task<Result<bool>> MarkConversationAsReadAsync(Guid conversationId, Guid userId)
        {
            _logger.LogInformation("Marking conversation {ConversationId} as read for user {UserId}", conversationId, userId);

            if (!await _messageRepository.IsParticipantInConversationAsync(conversationId, userId))
            {
                throw new ForbiddenException("You are not a participant in this conversation.");
            }

            await _messageRepository.MarkMessagesAsReadAsync(conversationId, userId);
            await _commonRepository.SaveChangesAsync();

            return Result<bool>.Success(true, "Conversation marked as read.");
        }
    }
}

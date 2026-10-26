/**************************************************************************************************************
 * Title: Introduction to Azure Queue Storage
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/storage/queues/storage-queues-introduction
 **************************************************************************************************************/
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ABCRetailWebApp.Services
{
    public class AzureQueueStorageService
    {
        private readonly QueueClient _queueClient;
        private readonly ILogger<AzureQueueStorageService> _logger;

        public AzureQueueStorageService(IConfiguration configuration, ILogger<AzureQueueStorageService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var connectionString = configuration.GetConnectionString("AzureStorage");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("AzureStorage connection string is missing.");

            const string queueName = "notifications";

            try
            {
                _queueClient = new QueueClient(connectionString, queueName);

                Task.Run(async () =>
                {
                    try
                    {
                        await _queueClient.CreateIfNotExistsAsync();
                        _logger.LogInformation("Queue client initialized successfully for queue: {QueueName}", queueName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to initialize queue: {QueueName}", queueName);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create QueueClient with connection string");
                throw;
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("Attempted to send empty or null message");
                return;
            }

            try
            {
                if (!await _queueClient.ExistsAsync())
                {
                    _logger.LogInformation("Queue does not exist, creating it");
                    await _queueClient.CreateIfNotExistsAsync();
                }

                await _queueClient.SendMessageAsync(message);
                _logger.LogDebug("Message sent successfully: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message: {Message}", message);
                throw;
            }
        }

        public async Task LogEntityAction(string entityType, string action, string entityId, string details = "")
        {
            if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(action) || string.IsNullOrEmpty(entityId))
            {
                _logger.LogWarning("Invalid parameters for LogEntityAction. EntityType: {EntityType}, Action: {Action}, EntityId: {EntityId}",
                    entityType, action, entityId);
                throw new ArgumentException("Entity type, action, and ID must be provided.");
            }

            // Use South African time for all log messages
            var localTime = DateTime.UtcNow.AddHours(2); // UTC+2 for South Africa
            string timestamp = localTime.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"{timestamp} | {entityType} {action} | ID: {entityId}";

            if (!string.IsNullOrEmpty(details))
                logMessage += $" | {details}";

            await SendMessageAsync(logMessage);
        }

        public async Task<List<string>> PeekMessagesAsync(int maxMessages = 50)
        {
            try
            {
                _logger.LogDebug("Attempting to peek {MaxMessages} messages", maxMessages);

                var queueExists = await _queueClient.ExistsAsync();
                if (!queueExists.Value)
                {
                    _logger.LogInformation("Queue does not exist, returning empty list");
                    return new List<string>();
                }

                if (maxMessages <= 0 || maxMessages > 32)
                {
                    _logger.LogWarning("Invalid maxMessages value: {MaxMessages}. Using default of 32", maxMessages);
                    maxMessages = 32;
                }

                var response = await _queueClient.PeekMessagesAsync(maxMessages);
                var messages = response.Value?.Select(m =>
                {
                    try
                    {
                        return m.Body?.ToString() ?? "Empty message";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to convert message body to string");
                        return "Invalid message format";
                    }
                }).ToList() ?? new List<string>();

                _logger.LogInformation("Successfully retrieved {MessageCount} messages from queue", messages.Count);
                return messages;
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure request failed while peeking messages. Status: {Status}, Error: {Error}",
                    ex.Status, ex.ErrorCode);
                throw new InvalidOperationException($"Failed to peek messages from Azure Queue: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while peeking messages");
                throw new InvalidOperationException($"Unexpected error while retrieving queue messages: {ex.Message}", ex);
            }
        }

        public async Task<List<QueueMessage>> ReceiveMessagesAsync(int maxMessages = 50, TimeSpan? visibilityTimeout = null)
        {
            try
            {
                _logger.LogDebug("Attempting to receive {MaxMessages} messages", maxMessages);

                var queueExists = await _queueClient.ExistsAsync();
                if (!queueExists.Value)
                {
                    _logger.LogInformation("Queue does not exist, returning empty list");
                    return new List<QueueMessage>();
                }

                if (maxMessages <= 0 || maxMessages > 32)
                {
                    maxMessages = Math.Min(maxMessages, 32);
                }

                var response = await _queueClient.ReceiveMessagesAsync(
                    maxMessages: maxMessages,
                    visibilityTimeout: visibilityTimeout ?? TimeSpan.FromMinutes(5));

                var messages = response.Value?.ToList() ?? new List<QueueMessage>();
                _logger.LogInformation("Successfully received {MessageCount} messages from queue", messages.Count);

                return messages;
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure request failed while receiving messages. Status: {Status}, Error: {Error}",
                    ex.Status, ex.ErrorCode);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while receiving messages");
                throw;
            }
        }

        public async Task DeleteMessageAsync(QueueMessage message)
        {
            try
            {
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                _logger.LogDebug("Successfully deleted message: {MessageId}", message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete message: {MessageId}", message.MessageId);
                throw;
            }
        }

        public async Task ClearMessagesAsync()
        {
            try
            {
                var queueExists = await _queueClient.ExistsAsync();
                if (queueExists.Value)
                {
                    await _queueClient.ClearMessagesAsync();
                    _logger.LogInformation("Successfully cleared all messages from queue");
                }
                else
                {
                    _logger.LogWarning("Attempted to clear messages from non-existent queue");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear queue messages");
                throw;
            }
        }

        public async Task<QueueProperties> GetQueuePropertiesAsync()
        {
            try
            {
                var response = await _queueClient.GetPropertiesAsync();
                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get queue properties");
                throw;
            }
        }

        public async Task<bool> QueueExistsAsync()
        {
            try
            {
                var response = await _queueClient.ExistsAsync();
                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if queue exists");
                return false;
            }
        }

        public async Task<List<string>> GetLatestMessagesAsync(int maxMessages = 32)
        {
            try
            {
                var allMessages = new List<string>();

                var properties = await GetQueuePropertiesAsync();
                var totalMessages = properties.ApproximateMessagesCount;

                _logger.LogInformation("Total messages in queue: {TotalCount}, requesting latest: {RequestCount}",
                    totalMessages, maxMessages);

                if (totalMessages == 0)
                {
                    return allMessages;
                }

                if (totalMessages <= maxMessages)
                {
                    var messages = await PeekMessagesAsync((int)totalMessages);
                    return messages.AsEnumerable().Reverse().ToList();
                }

                var latestMessages = new List<QueueMessage>();
                var messagesToSkip = Math.Max(0, (int)totalMessages - maxMessages);

                var receivedMessages = await ReceiveMessagesAsync(32, TimeSpan.FromSeconds(30));
                var allReceivedMessages = new List<QueueMessage>(receivedMessages);

                while (receivedMessages.Count == 32 && allReceivedMessages.Count < totalMessages)
                {
                    receivedMessages = await ReceiveMessagesAsync(32, TimeSpan.FromSeconds(30));
                    allReceivedMessages.AddRange(receivedMessages);

                    if (allReceivedMessages.Count >= 100) break;
                }

                var latestMessageBodies = allReceivedMessages
                    .TakeLast(maxMessages)
                    .Select(m => m.Body.ToString())
                    .Reverse()
                    .ToList();

                foreach (var msg in allReceivedMessages)
                {
                    try
                    {
                        await _queueClient.SendMessageAsync(msg.Body);
                        await DeleteMessageAsync(msg);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to re-queue message: {MessageId}", msg.MessageId);
                    }
                }

                _logger.LogInformation("Retrieved {Count} latest messages", latestMessageBodies.Count);
                return latestMessageBodies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest messages, falling back to standard peek");
                var fallbackMessages = await PeekMessagesAsync(maxMessages);
                return fallbackMessages.AsEnumerable().Reverse().ToList();
            }
        }
    }
}
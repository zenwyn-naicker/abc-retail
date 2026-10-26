using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCRetailWebApp.Controllers
{
    public class QueueMessagesController : Controller
    {
        private readonly AzureQueueStorageService _queueService;
        private readonly AzureFunctionsService _functionsService;
        private readonly ILogger<QueueMessagesController> _logger;

        public QueueMessagesController(AzureQueueStorageService queueService,
                                       AzureFunctionsService functionsService,
                                       ILogger<QueueMessagesController> logger)
        {
            _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
            _functionsService = functionsService ?? throw new ArgumentNullException(nameof(functionsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /QueueMessages/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var messages = await _functionsService.ReadQueueMessagesViaFunctionAsync();

                // Reverse once, then take first 32 
                messages = messages.AsEnumerable().Reverse().ToList();

                if (messages.Count == 0)
                {
                    TempData["Info"] = "No messages currently in the queue.";
                }
                else if (messages.Count > 32)
                {
                    TempData["Info"] = $"Showing the latest 32 messages of {messages.Count} total.";
                    messages = messages.Take(32).ToList();
                }

                _logger.LogInformation("Retrieved {Count} messages via function", messages.Count);

                return View(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving queue messages via function");
                TempData["Error"] = "Failed to retrieve queue messages. Please check the logs.";
                return View(new List<string>());
            }
        }

        // POST: /QueueMessages/TestMessage
        [HttpPost]
        public async Task<IActionResult> TestMessage()
        {
            try
            {
                var localTime = DateTime.UtcNow.AddHours(2); // South African time
                var testMessage = $"Test message sent at {localTime:yyyy-MM-dd HH:mm:ss} SAST";

                await _functionsService.WriteQueueMessageViaFunctionAsync(testMessage);

                TempData["Success"] = "Test message sent successfully via function!";
                _logger.LogInformation("Test message sent via function");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test message via function");
                TempData["Error"] = "Failed to send test message. Please check the logs.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            try
            {
                _logger.LogInformation("Attempting to clear queue messages");

                var queueExists = await _queueService.QueueExistsAsync();
                if (!queueExists)
                {
                    _logger.LogWarning("Attempted to clear non-existent queue");
                    TempData["Warning"] = "Queue does not exist. Nothing to clear.";
                    return RedirectToAction(nameof(Index));
                }

                await _queueService.ClearMessagesAsync();

                _logger.LogInformation("Successfully cleared queue messages");
                TempData["Success"] = "Queue cleared successfully!";
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure service error while clearing queue messages. Status: {Status}", ex.Status);
                TempData["Error"] = $"Azure service error: {ex.Message}. Please check your permissions.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error clearing queue messages");
                TempData["Error"] = "Failed to clear queue messages. Please check the logs for details.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /QueueMessages/Debug
        [HttpGet]
        public async Task<IActionResult> Debug()
        {
            try
            {
                var messages = await _functionsService.ReadQueueMessagesViaFunctionAsync();
                return Json(new
                {
                    MessageCount = messages.Count,
                    Messages = messages.Count > 3 ? messages.GetRange(0, 3) : messages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in debug view via function");
                return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }
    }
}

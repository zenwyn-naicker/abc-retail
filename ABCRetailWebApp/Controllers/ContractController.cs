using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCRetailWebApp.Controllers
{
    public class ContractController : Controller
    {
        private readonly AzureFunctionsService _functionsService;
        private readonly AzureFileShareService _fileShareService;
        private readonly ILogger<ContractController> _logger;

        public ContractController(
            AzureFunctionsService functionsService,
            AzureFileShareService fileShareService,
            ILogger<ContractController> logger)
        {
            _functionsService = functionsService;
            _fileShareService = fileShareService;
            _logger = logger;
        }

        // GET: /Contract/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var contractFiles = await _fileShareService.GetContractFilesAsync();
                return View(contractFiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contract files.");
                TempData["Error"] = "Could not load contract files.";
                return View(new List<string>());
            }
        }

        // POST: /Contract/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile contractFile)
        {
            if (contractFile == null || contractFile.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Upload via Azure Function
                var result = await _functionsService.SendFileViaFunctionAsync(contractFile);

                // Log contract upload via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Contract Uploaded: FileName={contractFile.FileName}, Size={contractFile.Length} bytes, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                );

                TempData["Success"] = "Contract uploaded successfully via function!";
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid contract file upload: {FileName}", contractFile.FileName);
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading contract: {FileName}", contractFile.FileName);
                TempData["Error"] = "Failed to upload contract. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Contract/Download?fileName=filename.pdf
        [HttpGet]
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name is required.");

            try
            {
                var fileStream = await _fileShareService.DownloadContractAsync(fileName);
                var contentType = GetContentTypeFromFileName(fileName);
                return File(fileStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading contract: {FileName}", fileName);
                TempData["Error"] = "File not found or could not be downloaded.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Contract/Delete?fileName=filename.pdf
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["Error"] = "File name is required for deletion.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                bool deleted = await _fileShareService.DeleteContractAsync(fileName);

                if (deleted)
                {
                    // Log contract deletion via Azure Function
                    await _functionsService.WriteQueueMessageViaFunctionAsync(
                        $"Contract Deleted: FileName={fileName}, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                    );

                    TempData["Success"] = $"Contract '{fileName}' deleted successfully!";
                }
                else
                {
                    TempData["Error"] = $"Contract '{fileName}' could not be found or was already deleted.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contract: {FileName}", fileName);
                TempData["Error"] = "Failed to delete contract. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static string GetContentTypeFromFileName(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                _ => "application/octet-stream",
            };
        }
    }
}
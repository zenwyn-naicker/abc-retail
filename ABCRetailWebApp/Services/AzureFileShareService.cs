/**************************************************************************************************************
 * Title: Introduction to Azure File Share
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/storage/files/storage-files-introduction
 **************************************************************************************************************/
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ABCRetailWebApp.Services
{
    public class AzureFileShareService
    {
        private readonly ShareServiceClient _shareServiceClient;

        public AzureFileShareService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AzureStorage") ??
                                   throw new ArgumentNullException("Azure Storage connection string is required");

            _shareServiceClient = new ShareServiceClient(connectionString);
        }

        // ---------------------------
        // CONTRACT OPERATIONS
        // ---------------------------
        public async Task UploadContractAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".txt" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Only document files are allowed for contracts");

            var shareClient = _shareServiceClient.GetShareClient("contracts");
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(file.FileName);

            await fileClient.CreateAsync(file.Length);
            await fileClient.UploadAsync(file.OpenReadStream());
        }

        public async Task<bool> DeleteContractAsync(string fileName)
        {
            var shareClient = _shareServiceClient.GetShareClient("contracts");
            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            var response = await fileClient.DeleteIfExistsAsync();
            return response.Value;
        }

        public async Task<List<string>> GetContractFilesAsync()
        {
            var shareClient = _shareServiceClient.GetShareClient("contracts");
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetRootDirectoryClient();
            var files = new List<string>();

            await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory) files.Add(item.Name);
            }

            return files;
        }

        public async Task<Stream> DownloadContractAsync(string fileName)
        {
            var shareClient = _shareServiceClient.GetShareClient("contracts");
            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            var download = await fileClient.DownloadAsync();
            return download.Value.Content;
        }

        // ---------------------------
        // GENERIC FILE OPERATIONS
        // ---------------------------
        public async Task UploadFileAsync(string shareName, string fileName, IFormFile file, string? directoryPath = null)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("File is required");

            var shareClient = _shareServiceClient.GetShareClient(shareName);
            await shareClient.CreateIfNotExistsAsync();

            ShareDirectoryClient directoryClient = !string.IsNullOrEmpty(directoryPath)
                ? shareClient.GetDirectoryClient(directoryPath)
                : shareClient.GetRootDirectoryClient();

            await directoryClient.CreateIfNotExistsAsync();

            var fileClient = directoryClient.GetFileClient(fileName);
            await fileClient.CreateAsync(file.Length);
            await fileClient.UploadAsync(file.OpenReadStream());
        }

        public async Task<List<string>> GetFilesAsync(string shareName, string? directoryPath = null)
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            await shareClient.CreateIfNotExistsAsync();

            ShareDirectoryClient directoryClient = !string.IsNullOrEmpty(directoryPath)
                ? shareClient.GetDirectoryClient(directoryPath)
                : shareClient.GetRootDirectoryClient();

            var files = new List<string>();
            await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory) files.Add(item.Name);
            }

            return files;
        }

        public async Task<bool> FileExistsAsync(string shareName, string fileName, string? directoryPath = null)
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            ShareDirectoryClient directoryClient = !string.IsNullOrEmpty(directoryPath)
                ? shareClient.GetDirectoryClient(directoryPath)
                : shareClient.GetRootDirectoryClient();

            var fileClient = directoryClient.GetFileClient(fileName);
            var response = await fileClient.ExistsAsync();
            return response.Value;
        }
    }
}
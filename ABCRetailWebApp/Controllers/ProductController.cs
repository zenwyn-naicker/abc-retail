using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCRetailWebApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly AzureTableStorageService _tableStorageService;
        private readonly AzureFunctionsService _functionsService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            AzureTableStorageService tableStorageService,
            AzureFunctionsService functionsService,
            ILogger<ProductController> logger)
        {
            _tableStorageService = tableStorageService;
            _functionsService = functionsService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _tableStorageService.GetProductsAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return NotFound();

            var product = await _tableStorageService.GetProductAsync(rowKey);
            if (product == null) return NotFound();

            return View(product);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductEntity product, IFormFile ImageFile)
        {
            if (!ModelState.IsValid) return View(product);

            try
            {
                product.PartitionKey = "Product";

                // Upload image via Azure Function if a file is provided
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var imageUrl = await _functionsService.WriteBlobViaFunctionAsync(ImageFile);
                    product.ImageUrl = imageUrl; // store returned URL in product entity
                }

                // Store product via Azure Function
                var functionResult = await _functionsService.StoreProductViaFunctionAsync(product);

                // Log creation via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Product Created: Name={product.Name}, Price=R{product.Price:F2}, Stock={product.Stock}"
                );

                TempData["Success"] = "Product added successfully via function!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product via function: {ProductName}", product.Name);
                TempData["Error"] = "Failed to add product.";
                return View(product);
            }
        }

        public async Task<IActionResult> Edit(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return NotFound();

            var product = await _tableStorageService.GetProductAsync(rowKey);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductEntity product, IFormFile ImageFile)
        {
            if (!ModelState.IsValid) return View(product);

            try
            {
                var existingProduct = await _tableStorageService.GetProductAsync(product.RowKey);
                if (existingProduct == null) return NotFound();

                var changes = new List<string>();

                if (existingProduct.Name != product.Name)
                    changes.Add($"Name: {existingProduct.Name} → {product.Name}");
                if (existingProduct.Price != product.Price)
                    changes.Add($"Price: R{existingProduct.Price:F2} → R{product.Price:F2}");
                if (existingProduct.Stock != product.Stock)
                    changes.Add($"Stock: {existingProduct.Stock} → {product.Stock}");
                if (existingProduct.Category != product.Category)
                    changes.Add($"Category: {existingProduct.Category} → {product.Category}");
                if (existingProduct.Description != product.Description)
                    changes.Add("Description Updated");

                // Upload via Azure Function if a new file is provided
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var imageUrl = await _functionsService.WriteBlobViaFunctionAsync(ImageFile);
                    existingProduct.ImageUrl = imageUrl;
                    changes.Add("Image Updated");
                }

                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Stock = product.Stock;
                existingProduct.Category = product.Category;
                existingProduct.Description = product.Description;

                await _tableStorageService.UpdateProductAsync(existingProduct);

                var changeDetails = changes.Count > 0 ? string.Join(", ", changes) : "No changes detected";

                // Log update via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Product Updated: RowKey={existingProduct.RowKey}, Changes={changeDetails}"
                );

                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductName}", product.Name);
                TempData["Error"] = "Failed to update product.";
                return View(product);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return RedirectToAction(nameof(Index));

            try
            {
                var product = await _tableStorageService.GetProductAsync(rowKey);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }

                await _tableStorageService.DeleteProductAsync(rowKey, product.ETag);

                // Log deletion via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Product Deleted: Name={product.Name}, Category={product.Category}"
                );

                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {RowKey}", rowKey);
                TempData["Error"] = "Failed to delete product.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

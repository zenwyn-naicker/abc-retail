using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ABCRetailWebApp.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AzureTableStorageService _tableStorageService;
        private readonly AzureFunctionsService _functionsService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            AzureTableStorageService tableStorageService,
            AzureFunctionsService functionsService,
            ILogger<CustomerController> logger)
        {
            _tableStorageService = tableStorageService;
            _functionsService = functionsService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _tableStorageService.GetCustomersAsync();
            return View(customers);
        }

        public async Task<IActionResult> Details(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return NotFound();

            var customer = await _tableStorageService.GetCustomerAsync(rowKey);
            if (customer == null) return NotFound();

            return View(customer);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerEntity customer)
        {
            if (!ModelState.IsValid) return View(customer);

            try
            {
                customer.PartitionKey = "Customer";

                // Store customer via Azure Function
                var functionResult = await _functionsService.StoreCustomerViaFunctionAsync(customer);

                // Log via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Customer Created: Name={customer.Name}, Email={customer.Email}, Phone={customer.Phone}"
                );

                TempData["Success"] = "Customer added successfully via function!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding customer via function: {CustomerName}", customer.Name);
                TempData["Error"] = "Failed to add customer.";
                return View(customer);
            }
        }

        public async Task<IActionResult> Edit(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return NotFound();

            var customer = await _tableStorageService.GetCustomerAsync(rowKey);
            if (customer == null) return NotFound();

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerEntity customer)
        {
            if (!ModelState.IsValid) return View(customer);

            try
            {
                var existingCustomer = await _tableStorageService.GetCustomerAsync(customer.RowKey);
                if (existingCustomer == null) return NotFound();

                var changes = new List<string>();

                if (existingCustomer.Name != customer.Name)
                    changes.Add($"Name: {existingCustomer.Name} → {customer.Name}");
                if (existingCustomer.Email != customer.Email)
                    changes.Add($"Email: {existingCustomer.Email} → {customer.Email}");
                if (existingCustomer.Phone != customer.Phone)
                    changes.Add($"Phone: {existingCustomer.Phone ?? "None"} → {customer.Phone ?? "None"}");
                if (existingCustomer.Address != customer.Address)
                    changes.Add($"Address: {(string.IsNullOrEmpty(existingCustomer.Address) ? "None" : "Updated")}");

                existingCustomer.Name = customer.Name;
                existingCustomer.Email = customer.Email;
                existingCustomer.Phone = customer.Phone;
                existingCustomer.Address = customer.Address;

                await _tableStorageService.UpdateCustomerAsync(existingCustomer);

                var changeDetails = changes.Any() ? string.Join(", ", changes) : "No changes detected";

                // Log via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Customer Updated: RowKey={existingCustomer.RowKey}, Changes={changeDetails}"
                );

                TempData["Success"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer: {CustomerName}", customer.Name);
                TempData["Error"] = "Failed to update customer.";
                return View(customer);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return RedirectToAction(nameof(Index));

            try
            {
                var customer = await _tableStorageService.GetCustomerAsync(rowKey);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                await _tableStorageService.DeleteCustomerAsync(rowKey, customer.ETag);

                // Log via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Customer Deleted: Name={customer.Name}, Email={customer.Email}"
                );

                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {RowKey}", rowKey);
                TempData["Error"] = "Failed to delete customer.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

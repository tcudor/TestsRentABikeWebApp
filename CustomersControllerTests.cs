using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RentABikeWebApp.Controllers;
using RentABikeWebApp.Data;
using RentABikeWebApp.Models;
using RentABikeWebApp.Data.Services;
using System.Threading.Tasks;

namespace TestsRentABikeWebApp
{
    [TestClass]
    public class CustomersControllerTests
    {
        private ApplicationDbContext _context;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            SeedDatabase();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private void SeedDatabase()
        {
            var customers = new[]
            {
                new Customer { Id = 1, Name = "John Doe", Email = "john@example.com" },
                new Customer { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
            };
            _context.Customers.AddRange(customers);
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task Index_ReturnsViewResultWithAllCustomers()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var result = await controller.Index() as ViewResult;
            Assert.IsNotNull(result);
            var model = result.Model as IEnumerable<Customer>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count());
        }

        [TestMethod]
        public async Task Create_Post_ReturnsRedirectToActionResult_WhenModelStateIsValid()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var customer = new Customer { Name = "John Doe", Email = "john@example.com" };
            var result = await controller.Create(customer) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
        }

        [TestMethod]
        public async Task Create_Post_ReturnsViewResultWithModel_WhenModelStateIsInvalid()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var customer = new Customer { Name = "John Doe" };
            controller.ModelState.AddModelError("Email", "Email is required");
            var result = await controller.Create(customer) as ViewResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(customer, result.Model);
        }

        [TestMethod]
        public async Task Details_ReturnsViewResultWithCustomer_WhenCustomerExists()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var result = await controller.Details(1) as ViewResult;
            Assert.IsNotNull(result);
            var model = result.Model as Customer;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Id);
        }

        [TestMethod]
        public async Task Details_ReturnsNotFound_WhenCustomerDoesNotExist()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var result = await controller.Details(100) as ViewResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("NotFound", result.ViewName);
        }

        [TestMethod]
        public async Task Edit_ReturnsNotFound_WhenCustomerIsNull()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var result = await controller.Edit(100);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.AreEqual("NotFound", viewResult.ViewName);
        }

        [TestMethod]
        public async Task Edit_ReturnsViewResult_WhenCustomerExists()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var result = await controller.Edit(1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
        }

        [TestMethod]
        public async Task Edit_UpdatesCustomerInDatabase_WhenModelStateIsValid()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var existingCustomer = await _context.Customers.FindAsync(1);
            existingCustomer.Email = "test@gmail.com";
            existingCustomer.Name = "Test";
            var result = await controller.Edit(existingCustomer.Id, existingCustomer) as RedirectToActionResult;
            var updatedCustomer = await _context.Customers.FindAsync(1);
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            var editedCustomer = await _context.Customers.FindAsync(existingCustomer.Id);
            Assert.AreEqual(updatedCustomer.Email, editedCustomer.Email);
            Assert.AreEqual(updatedCustomer.Name, editedCustomer.Name);
        }

        [TestMethod]
        public async Task Delete_ReturnsNotFound_WhenCustomerIsNull()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var result = await controller.Delete(100);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.AreEqual("NotFound", viewResult.ViewName);
        }

        [TestMethod]
        public async Task Delete_ReturnsViewResult_WhenCustomerExists()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var result = await controller.Delete(1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
        }

        [TestMethod]
        public async Task DeleteConfirmed_DeletesCustomerFromDatabase()
        {
            var controller = new CustomersController(new CustomersService(_context));
            await controller.DeleteConfirmed(1);
            var deletedCustomer = await _context.Customers.FindAsync(1);
            Assert.IsNull(deletedCustomer);
        }

        [TestMethod]
        public async Task Edit_ReturnsNotFound_WhenIdIsInvalid()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var result = await controller.Edit(-1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.AreEqual("NotFound", viewResult.ViewName);
        }

        [TestMethod]
        public async Task Delete_ReturnsNotFound_WhenIdIsInvalid()
        {
            var controller = new CustomersController(new CustomersService(_context));
            var result = await controller.Delete(-1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.AreEqual("NotFound", viewResult.ViewName);
        }
    }
}

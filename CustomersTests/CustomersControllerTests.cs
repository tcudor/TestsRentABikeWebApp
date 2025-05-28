using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RentABikeWebApp.Controllers;
using RentABikeWebApp.Data;
using RentABikeWebApp.Data.Services;
using RentABikeWebApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TestsRentABikeWebApp.CustomersTests
{
    [TestClass]
    public class CustomersControllerTests
    {
        private ApplicationDbContext _context;
        private ICustomersService _service;
        private UserManager<IdentityUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _context.Customers.AddRange(new[]
            {
                new Customer { Id = 1, Name = "John Doe", Email = "john@example.com" },
                new Customer { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
            });
            _context.SaveChanges();

            var userStore = new UserStore<IdentityUser>(_context);
            _userManager = new UserManager<IdentityUser>(
                userStore,
                null!,
                new PasswordHasher<IdentityUser>(),
                Array.Empty<IUserValidator<IdentityUser>>(),
                Array.Empty<IPasswordValidator<IdentityUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null!,
                null!);

            var roleStore = new RoleStore<IdentityRole>(_context);
            _roleManager = new RoleManager<IdentityRole>(
                roleStore,
                [new RoleValidator<IdentityRole>()],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null!);

            if (!_roleManager.RoleExistsAsync("Client").Result)
                _roleManager.CreateAsync(new IdentityRole("Client")).Wait();
            _service = new CustomersService(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task Index_ReturnsViewResultWithAllCustomers()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            var result = await controller.Index() as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as IEnumerable<Customer>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count());
        }

        [TestMethod]
        public async Task Create_Post_ReturnsRedirectToActionResult_WhenModelStateIsValid()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            var customer = new Customer { Name = "Alice", Email = "alice@example.com" };

            var result = await controller.Create(customer) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
        }

        [TestMethod]
        public async Task Create_Post_ReturnsViewResultWithModel_WhenModelStateIsInvalid()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            var customer = new Customer { Name = "Bob" };
            controller.ModelState.AddModelError("Email", "Email is required");

            var result = await controller.Create(customer) as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(customer, result.Model);
        }

        [TestMethod]
        public async Task Create_Post_CreatesIdentityUserAndLinksToCustomer()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            var email = "newuser@example.com";
            var customer = new Customer { Name = "New User", Email = email };

            var result = await controller.Create(customer) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            var createdCust = _context.Customers.Single(c => c.Email == email);
            Assert.IsFalse(string.IsNullOrEmpty(createdCust.UserId));
            var user = await _context.Users.FindAsync(createdCust.UserId);
            Assert.IsNotNull(user);
            Assert.AreEqual(email, user.Email);
        }

        [TestMethod]
        public async Task Details_ReturnsViewResultWithCustomer_WhenCustomerExists()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            var result = await controller.Details(1) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as Customer;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Id);
        }

        [TestMethod]
        public async Task Details_ReturnsNotFound_WhenCustomerDoesNotExist()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            var result = await controller.Details(100) as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("NotFound", result.ViewName);
        }

        [TestMethod]
        public async Task Edit_ReturnsNotFound_WhenCustomerIsNull()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            var result = await controller.Edit(100);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("NotFound", ((ViewResult)result).ViewName);
        }

        [TestMethod]
        public async Task Edit_ReturnsViewResult_WhenCustomerExists()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            var result = await controller.Edit(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsNull(((ViewResult)result).ViewName);
        }

        [TestMethod]
        public async Task Edit_UpdatesCustomerInDatabase_WhenModelStateIsValid()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            var existingCustomer = await _context.Customers.FindAsync(1);
            existingCustomer.Email = "changed@example.com";
            existingCustomer.Name = "Changed Name";

            var result = await controller.Edit(existingCustomer.Id, existingCustomer)
                             as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            var updated = await _context.Customers.FindAsync(1);
            Assert.AreEqual("changed@example.com", updated.Email);
            Assert.AreEqual("Changed Name", updated.Name);
        }

        [TestMethod]
        public async Task Delete_ReturnsNotFound_WhenCustomerIsNull()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            var result = await controller.Delete(100);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.AreEqual("NotFound", ((ViewResult)result).ViewName);
        }

        [TestMethod]
        public async Task Delete_ReturnsViewResult_WhenCustomerExists()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            var result = await controller.Delete(1);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsNull(((ViewResult)result).ViewName);
        }

        [TestMethod]
        public async Task DeleteConfirmed_DeletesCustomerFromDatabase()
        {
            var controller = new CustomersController(_service, _userManager, _roleManager);
            await controller.DeleteConfirmed(1);

            var deleted = await _context.Customers.FindAsync(1);
            Assert.IsNull(deleted);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using RentABikeWebApp.Data.Services;
using RentABikeWebApp.Data;
using RentABikeWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestsRentABikeWebApp.CustomersTests
{
    [TestClass]
    public class CustomersServiceTests
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
                new Customer { Id = 1, Name = "John Doe", Email = "john@example.com", Phone = "123456789", IdCode = "123", IdSeries = "ABC" },
                new Customer { Id = 2, Name = "Jane Doe", Email = "jane@example.com", Phone = "987654321", IdCode = "456", IdSeries = "DEF" }
            };
            _context.Customers.AddRange(customers);
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task GetAllAsync_ReturnsAllCustomers()
        {
            var service = new CustomersService(_context);
            var result = await service.GetAllAsync();
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsCustomer_WhenExists()
        {
            var service = new CustomersService(_context);
            var result = await service.GetByIdAsync(1);
            Assert.IsNotNull(result);
            Assert.AreEqual("John Doe", result.Name);
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
        {
            var service = new CustomersService(_context);
            var result = await service.GetByIdAsync(100);
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task AddAsync_AddsCustomerToDatabase()
        {
            var service = new CustomersService(_context);
            var newCustomer = new Customer { Name = "New Customer", Email = "new@example.com", Phone = "999999999", IdCode = "789", IdSeries = "GHI" };
            await service.AddAsync(newCustomer);
            var customers = await _context.Customers.ToListAsync();
            Assert.AreEqual(3, customers.Count);
            Assert.IsTrue(customers.Any(c => c.Name == "New Customer"));
        }

        [TestMethod]
        public async Task UpdateAsync_UpdatesCustomerInDatabase()
        {
            var service = new CustomersService(_context);
            var existingCustomer = await _context.Customers.FindAsync(1);
            existingCustomer.Name = "Updated Name";
            await service.UpdateAsync(1, existingCustomer);
            var updatedCustomer = await _context.Customers.FindAsync(1);
            Assert.IsNotNull(updatedCustomer);
            Assert.AreEqual("Updated Name", updatedCustomer.Name);
        }

        [TestMethod]
        public async Task DeleteAsync_DeletesCustomerFromDatabase()
        {
            var service = new CustomersService(_context);
            await service.DeleteAsync(1);
            var deletedCustomer = await _context.Customers.FindAsync(1);
            Assert.IsNull(deletedCustomer);
        }
    }
}

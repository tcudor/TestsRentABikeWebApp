using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RentABikeWebApp.Data;
using RentABikeWebApp.Data.Services;
using RentABikeWebApp.Data.ViewModels;
using RentABikeWebApp.Models;

namespace TestsRentABikeWebApp.ReservationsTests
{
    [TestClass]
    public class ReservationsServiceTests
    {
        private ApplicationDbContext? _context;
        private ReservationsService? _service;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            SeedDatabase();
            _service = new ReservationsService(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private void SeedDatabase()
        {
            var now = DateTime.Now;
            var bikes = new[]
            {
                new Bike { Id = 1, Type = BikeType.Simple, PricePerHour = 10.00m },
                new Bike { Id = 2, Type = BikeType.Mountain, PricePerHour = 15.00m }
            };
            _context.Bikes.AddRange(bikes);

            var customers = new[]
            {
                new Customer { Id = 1, Name = "John Doe", UserId = "user1" },
                new Customer { Id = 2, Name = "Jane Smith", UserId = "user2" }
            };
            _context.Customers.AddRange(customers);

            var reservations = new[]
            {
                new Reservation { Id = 1, BikeId = 1, CustomerId = 1, StartDate = now.AddDays(1), EndDate = now.AddDays(2) },
                new Reservation { Id = 2, BikeId = 2, CustomerId = 2, StartDate = now.AddDays(3), EndDate = now.AddDays(4) }
            };
            _context.Reservations.AddRange(reservations);
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task GetAllAsync_ReturnsAllReservations()
        {
            var result = await _service.GetAllAsync();
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsReservation_WhenExists()
        {
            var result = await _service.GetByIdAsync(1);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Id);
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
        {
            var result = await _service.GetByIdAsync(99);
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task IsBikeAvailableAsync_ReturnsTrue_WhenNoOverlap()
        {
            var r = _context.Reservations.Single(r => r.Id == 1);
            var start = r.EndDate;
            var end = r.EndDate.AddHours(1);
            var available = await _service.IsBikeAvailableAsync(r.BikeId, start, end);
            Assert.IsTrue(available);
        }

        [TestMethod]
        public async Task IsBikeAvailableAsync_ReturnsFalse_WhenOverlapStartInside()
        {
            var r = _context.Reservations.Single(r => r.Id == 1);
            var start = r.StartDate.AddHours(0.5);
            var end = r.EndDate.AddHours(1);
            var available = await _service.IsBikeAvailableAsync(r.BikeId, start, end);
            Assert.IsFalse(available);
        }

        [TestMethod]
        public async Task IsBikeAvailableAsync_ReturnsFalse_WhenWrapsExisting()
        {
            var r = _context.Reservations.Single(r => r.Id == 1);
            var start = r.StartDate.AddHours(-1);
            var end = r.EndDate.AddHours(1);
            var available = await _service.IsBikeAvailableAsync(r.BikeId, start, end);
            Assert.IsFalse(available);
        }

        [TestMethod]
        public async Task GetActiveReservationsForBikeAsync_ReturnsCorrectDtos()
        {
            var result = await _service.GetActiveReservationsForBikeAsync(1);
            Assert.AreEqual(1, result.Count());
            var dto = result.Single();
            var r = _context.Reservations.Single(r2 => r2.Id == 1);
            Assert.AreEqual(r.StartDate.ToString("dd/MM/yyyy HH:mm"), dto.StartDate);
            Assert.AreEqual(r.EndDate.ToString("dd/MM/yyyy HH:mm"), dto.EndDate);
        }

        [TestMethod]
        public async Task GetReservationFormValuesAsync_Admin_ReturnsAllBikesAndCustomers()
        {
            var vm = await _service.GetReservationFormValuesAsync(
                selectedBikeId: 1,
                currentUserId: null,
                isAdmin: true);

            Assert.AreEqual(2, vm.Bikes.Count);
            Assert.AreEqual(2, vm.Customers.Count);
            Assert.AreEqual(1, vm.ActiveReservations.Count());
            Assert.AreEqual(10.00m, vm.PricePerHour);
            Assert.AreEqual(1, vm.SelectedBikeId);
        }

        [TestMethod]
        public async Task GetReservationFormValuesAsync_Client_ReturnsOnlyTheirCustomer()
        {
            var vm = await _service.GetReservationFormValuesAsync(
                selectedBikeId: 1,
                currentUserId: "user1",
                isAdmin: false);

            Assert.AreEqual(2, vm.Bikes.Count);
            Assert.AreEqual(1, vm.Customers.Count);
            Assert.AreEqual("John Doe", vm.Customers.Single().Text);
        }

        [TestMethod]
        public async Task GetReservationFormValuesAsync_ExcludesSpecifiedReservation()
        {
            var vm = await _service.GetReservationFormValuesAsync(
                selectedBikeId: 1,
                currentUserId: null,
                isAdmin: true,
                excludeReservationId: 1);

            Assert.AreEqual(0, vm.ActiveReservations.Count());
        }

        [TestMethod]
        public async Task AddUpdateDelete_WorkAsExpected()
        {
            var initial = (await _service.GetAllAsync()).Count();
            var newRes = new Reservation
            {
                BikeId = 1,
                CustomerId = 1,
                StartDate = DateTime.Now.AddDays(5),
                EndDate = DateTime.Now.AddDays(6)
            };
            await _service.AddAsync(newRes);
            Assert.AreEqual(initial + 1, (await _service.GetAllAsync()).Count());

            var added = _context.Reservations.OrderBy(r => r.Id).Last();
            added.EndDate = added.EndDate.AddDays(1);
            await _service.UpdateAsync(added.Id, added);
            var updated = await _service.GetByIdAsync(added.Id);
            Assert.AreEqual(added.EndDate.Date, updated.EndDate.Date);

            await _service.DeleteAsync(added.Id);
            Assert.IsNull(await _service.GetByIdAsync(added.Id));
        }

        [TestMethod]
        public async Task GetBikePriceAsync_ReturnsCorrectPrice_WhenBikeExists()
        {
            var svc = new ReservationsService(_context);
            var price = await svc.GetBikePriceAsync(1);
            Assert.AreEqual(10m, price);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public async Task GetBikePriceAsync_ThrowsKeyNotFound_WhenBikeDoesNotExist()
        {
            var svc = new ReservationsService(_context);
            await svc.GetBikePriceAsync(999); 
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RentABikeWebApp.Data;
using RentABikeWebApp.Data.Services;
using RentABikeWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestsRentABikeWebApp.ReservationsTests
{
    [TestClass]
    public class ReservationsServiceTests
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
            var bikes = new[]
            {
                new Bike { Id = 1, Type = BikeType.Simple, PricePerHour = 10.00m },
                new Bike { Id = 2, Type = BikeType.Mountain, PricePerHour = 15.00m }
            };
            _context.Bikes.AddRange(bikes);

            var customers = new[]
            {
                new Customer { Id = 1, Name = "John Doe" },
                new Customer { Id = 2, Name = "Jane Smith" }
            };
            _context.Customers.AddRange(customers);

            var reservations = new[]
            {
                new Reservation { Id = 1, BikeId = 1, CustomerId = 1, StartDate = DateTime.Now.AddDays(1), EndDate = DateTime.Now.AddDays(2) },
                new Reservation { Id = 2, BikeId = 2, CustomerId = 2, StartDate = DateTime.Now.AddDays(3), EndDate = DateTime.Now.AddDays(4) }
            };
            _context.Reservations.AddRange(reservations);

            _context.SaveChanges();
        }

        [TestMethod]
        public async Task GetAllAsync_ReturnsAllReservations()
        {
            var service = new ReservationsService(_context);
            var result = await service.GetAllAsync();
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsReservation_WhenExists()
        {
            var service = new ReservationsService(_context);
            var result = await service.GetByIdAsync(1);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Id);
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
        {
            var service = new ReservationsService(_context);
            var result = await service.GetByIdAsync(100);
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetNewReservationDropdownsValues_ReturnsDropdownValues()
        {
            var service = new ReservationsService(_context);
            var result = await service.GetNewReservationDropdownsValues();
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Bikes.Count());
            Assert.AreEqual(2, result.Customers.Count());
        }

        [TestMethod]
        public async Task IsBikeAvailableAsync_ReturnsTrue_WhenBikeIsAvailable()
        {
            var service = new ReservationsService(_context);
            var result = await service.IsBikeAvailableAsync(1, DateTime.Now.AddDays(5), DateTime.Now.AddDays(6));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task IsBikeAvailableAsync_ReturnsFalse_WhenBikeIsNotAvailable()
        {
            var service = new ReservationsService(_context);
            var result = await service.IsBikeAvailableAsync(1, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task GetActiveReservationsForBikeAsync_ReturnsActiveReservations()
        {
            var service = new ReservationsService(_context);
            var result = await service.GetActiveReservationsForBikeAsync(1);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        [TestMethod]
        public async Task UpdateAsync_UpdatesReservationInDatabase()
        {
            var service = new ReservationsService(_context);
            var existingReservation = await service.GetByIdAsync(1);
            existingReservation.StartDate = DateTime.Now.AddDays(10);
            await service.UpdateAsync(1, existingReservation);
            var updatedReservation = await service.GetByIdAsync(1);
            Assert.IsNotNull(updatedReservation);
            Assert.AreEqual(DateTime.Now.AddDays(10).Date, updatedReservation.StartDate.Date);
        }

        [TestMethod]
        public async Task DeleteAsync_DeletesReservationFromDatabase()
        {
            var service = new ReservationsService(_context);
            await service.DeleteAsync(1);
            var deletedReservation = await service.GetByIdAsync(1);
            Assert.IsNull(deletedReservation);
        }

        [TestMethod]
        public async Task AddAsync_AddsReservationToDatabase()
        {
            var service = new ReservationsService(_context);
            var reservations = await service.GetAllAsync();
            var count= reservations.Count();
            var newReservation = new Reservation
            {
                StartDate = DateTime.Now.AddDays(5),
                EndDate = DateTime.Now.AddDays(7),
                BikeId = 1,
                CustomerId = 1
            };
            await service.AddAsync(newReservation);
            reservations = await service.GetAllAsync();
            Assert.AreEqual(count+1, reservations.Count());

        }

    }
}

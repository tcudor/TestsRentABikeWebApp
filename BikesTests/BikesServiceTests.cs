using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RentABikeWebApp.Data;
using RentABikeWebApp.Data.Services;
using RentABikeWebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestsRentABikeWebApp.BikesTests
{
    [TestClass]
    public class BikesServiceTests
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
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task GetAllAsync_ReturnsAllBikes()
        {
            var service = new BikesService(_context);
            var result = await service.GetAllAsync();
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsBike_WhenExists()
        {
            var service = new BikesService(_context);
            var result = await service.GetByIdAsync(1);
            Assert.IsNotNull(result);
            Assert.AreEqual(BikeType.Simple, result.Type);
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
        {
            var service = new BikesService(_context);
            var result = await service.GetByIdAsync(100);
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task AddAsync_AddsBikeToDatabase()
        {
            var service = new BikesService(_context);
            var newBike = new Bike { Type = BikeType.Simple, PricePerHour = 20.00m };
            await service.AddAsync(newBike);
            var bikes = await _context.Bikes.ToListAsync();
            Assert.AreEqual(3, bikes.Count);
            Assert.IsTrue(bikes.Any(b => b.Type == BikeType.Simple));
        }

        [TestMethod]
        public async Task UpdateAsync_UpdatesBikeInDatabase()
        {
            var service = new BikesService(_context);
            var existingBike = await _context.Bikes.FindAsync(1);
            existingBike.Type = BikeType.Mountain;
            await service.UpdateAsync(1, existingBike);
            var updatedBike = await _context.Bikes.FindAsync(1);
            Assert.IsNotNull(updatedBike);
            Assert.AreEqual(BikeType.Mountain, updatedBike.Type);
        }

        [TestMethod]
        public async Task DeleteAsync_DeletesBikeFromDatabase()
        {
            var service = new BikesService(_context);
            await service.DeleteAsync(1);
            var deletedBike = await _context.Bikes.FindAsync(1);
            Assert.IsNull(deletedBike);
        }

        [TestMethod]
        public void UpdateBikeStatusBasedOnReservations_ActiveReservation_StatusIsUnavailable()
        {
            // Arrange
            var reservations = new List<Reservation> { new Reservation { StartDate = DateTime.Now.AddDays(-1), EndDate = DateTime.Now.AddDays(1) } };
            var bike = new Bike
            {
                Id = 3,
                Status = StatusType.Available,
                Reservations = reservations
            };
            _context.Bikes.Add(bike);
            _context.SaveChanges();

            var service = new BikesService(_context);
            service.UpdateBikeStatusBasedOnReservations(bike);
            Assert.AreEqual(StatusType.Unavailable, bike.Status);
        }

        [TestMethod]
        public void UpdateBikeStatusBasedOnReservations_NoActiveReservation_StatusIsAvailable()
        {
            var reservations = new List<Reservation> { new Reservation { StartDate = DateTime.Now.AddDays(-2), EndDate = DateTime.Now.AddDays(-1) } };
            var bike = new Bike
            {
                Id = 4,
                Status = StatusType.Unavailable,
                Reservations = reservations
            };
            _context.Bikes.Add(bike);
            _context.SaveChanges();

            var service = new BikesService(_context);
            service.UpdateBikeStatusBasedOnReservations(bike);
            Assert.AreEqual(StatusType.Available, bike.Status);
        }
    }
}

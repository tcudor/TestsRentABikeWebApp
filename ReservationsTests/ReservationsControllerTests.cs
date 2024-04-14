using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RentABikeWebApp.Controllers;
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
    public class ReservationsControllerTests
    {
        private ReservationsController _controller;
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

            var reservationsService = new ReservationsService(_context);
            _controller = new ReservationsController(reservationsService);
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
                new Bike { Id = 2, Type = BikeType.Hybrid, PricePerHour = 8.00m }
            };
            _context.Bikes.AddRange(bikes);

            var customers = new[]
            {
                new Customer { Id = 1, Name = "John Doe" },
                new Customer { Id = 2, Name = "Jane Doe" }
            };
            _context.Customers.AddRange(customers);

            var reservations = new[]
            {
                new Reservation{Id=1, EndDate=DateTime.Now, StartDate=DateTime.Now.AddHours(2),BikeId=1,CustomerId=1},
                 new Reservation{Id=2, EndDate=DateTime.Now, StartDate=DateTime.Now.AddHours(4),BikeId=2,CustomerId=2}
            };
            _context.Reservations.AddRange(reservations);
            _context.SaveChanges();
        }


        [TestMethod]
        public async Task Index_ReturnsViewResultWithAllReservations()
        {
            var controller = new ReservationsController(new ReservationsService(_context));
            var result = await controller.Index() as ViewResult;
            Assert.IsNotNull(result);
            var model = result.Model as IEnumerable<Reservation>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count());
        }


        [TestMethod]
        public async Task Create_POST_RedirectsToIndex_WhenModelStateIsValid()
        {
            var reservation = new Reservation
            {
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(2),
                BikeId = 1,
                CustomerId = 1
            };

            var result = await _controller.Create(reservation) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
        }

        [TestMethod]
        public async Task Edit_ReturnsNotFound_WhenReservatinonIsNull()
        {
            var controller = new ReservationsController(new ReservationsService(_context));
            var result = await controller.Edit(-1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.AreEqual("NotFound", viewResult.ViewName);
        }

        [TestMethod]
        public async Task Edit_ReturnsViewResult_WhenReservatinonExists()
        {
            var controller = new ReservationsController(new ReservationsService(_context));
            var result = await controller.Edit(1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
        }

        [TestMethod]
        public async Task Details_ReturnsViewResult_WithReservationDetails()
        {
            var controller = new ReservationsController(new ReservationsService(_context));
            var result = await controller.Details(1) as ViewResult;
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Model);
            Assert.IsInstanceOfType(result.Model, typeof(Reservation));
        }

        [TestMethod]
        public async Task Delete_GET_ReturnsViewResult_WhenReservationExists()
        {
            var controller = new ReservationsController(new ReservationsService(_context));
            var result = await controller.Delete(1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
        }

        [TestMethod]
        public async Task Delete_GET_ReturnsViewResult_WhenReservatinonIsNull()
        {
            var controller = new ReservationsController(new ReservationsService(_context));
            var result = await controller.Delete(-1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.AreEqual("NotFound", viewResult.ViewName);
        }

        [TestMethod]
        public async Task DeleteConfirmed_DeletesReservationFromDatabase()
        {
            var controller = new ReservationsController(new ReservationsService(_context));
            await controller.DeleteConfirmed(1);
            var deletedReservation = await _context.Reservations.FindAsync(1);
            Assert.IsNull(deletedReservation);
        }



    }
}

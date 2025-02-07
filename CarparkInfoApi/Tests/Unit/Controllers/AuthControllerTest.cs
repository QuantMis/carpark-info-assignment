using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using CarparkInfoApi.Controllers;
using CarparkInfoApi.Models;
using CarparkInfoApi.Services;
using CarparkInfoApi.Persistence;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarparkInfoApi.Tests.Unit.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly Mock<IPasswordHasher<User>> _mockPasswordHasher;
        private readonly Mock<JwtService> _mockJwtService;
        private readonly AuthController _controller;
        private readonly List<User> _users;

        public AuthControllerTests()
        {
            _mockContext = new Mock<ApplicationDbContext>();
            _mockPasswordHasher = new Mock<IPasswordHasher<User>>();
            _mockJwtService = new Mock<JwtService>();

            var mockUserDbSet = new Mock<DbSet<User>>();
            _users = new List<User>
        {
            new User { UserName = "testuser", PasswordHash = "hashedpassword" }
        };

            mockUserDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(_users.AsQueryable().Provider);
            mockUserDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(_users.AsQueryable().Expression);
            mockUserDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(_users.AsQueryable().ElementType);
            mockUserDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(_users.AsQueryable().GetEnumerator());

            _mockContext.Setup(c => c.Users).Returns(mockUserDbSet.Object);

            _controller = new AuthController(_mockContext.Object, _mockPasswordHasher.Object, _mockJwtService.Object);
        }

        [Fact]
        public void Login_ValidUser_ReturnsToken()
        {
            // Arrange
            var loginUser = new User { UserName = "testuser", PasswordHash = "password123" };
            _mockPasswordHasher.Setup(p => p.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(PasswordVerificationResult.Success);
            _mockJwtService.Setup(j => j.GenerateJwtToken(It.IsAny<User>())).Returns("mocked_token");

            // Act
            var result = _controller.Login(loginUser) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Contains("Token", result.Value.ToString());
        }

        [Fact]
        public void Login_InvalidUser_ReturnsUnauthorized()
        {
            // Arrange
            var loginUser = new User { UserName = "wronguser", PasswordHash = "password123" };

            // Act
            var result = _controller.Login(loginUser) as UnauthorizedObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
            Assert.Equal("Invalid username or password", result.Value);
        }

        [Fact]
        public void GetUserProfile_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var mockUser = new Mock<ClaimsPrincipal>();
            mockUser.Setup(u => u.Identity.Name).Returns("nonexistentuser");
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = mockUser.Object } };

            // Act
            var result = _controller.GetUserProfile() as NotFoundObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("User not found.", result.Value);
        }
    }
}
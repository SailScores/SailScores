using System.Reflection;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Controllers;
using SailScores.Web.Authorization;
using Xunit;
using System;

namespace SailScores.Test.Unit.Web.Controllers
{
    public class AuthorizationAttributeTests
    {
        [Theory]
        [InlineData(typeof(BoatClassController), nameof(BoatClassController.Create), AuthorizationPolicies.ClubAdmin)]
        [InlineData(typeof(BoatClassController), nameof(BoatClassController.Edit), AuthorizationPolicies.ClubAdmin)]
        [InlineData(typeof(BoatClassController), nameof(BoatClassController.Delete), AuthorizationPolicies.ClubAdmin)]
        [InlineData(typeof(CompetitorController), nameof(CompetitorController.Create), AuthorizationPolicies.RaceScorekeeper)]
        public void Action_HasCorrectAuthorizationPolicy(Type controllerType, string methodName, string expectedPolicy)
        {
            // Arrange
            var method = controllerType.GetMethods()
                .FirstOrDefault(m => m.Name == methodName && m.GetCustomAttributes<HttpPostAttribute>().Any() == false); // check GET version primarily
            
            if (method == null)
            {
                method = controllerType.GetMethod(methodName);
            }

            // Act
            var authorizeAttribute = method.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.NotNull(authorizeAttribute);
            Assert.Equal(expectedPolicy, authorizeAttribute.Policy);
        }

        [Theory]
        [InlineData(typeof(BoatClassController))]
        [InlineData(typeof(CompetitorController))]
        public void Controller_HasAuthorizeAttribute(Type controllerType)
        {
            // Act
            var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.NotNull(authorizeAttribute);
        }
    }
}

﻿using System.Net;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using TestApi.Common.Builders;
using TestApi.Common.Data;
using TestApi.Contract.Responses;
using TestApi.DAL.Queries;
using TestApi.Domain;
using TestApi.Domain.Enums;
using TestApi.Services.Clients.UserApiClient;

namespace TestApi.UnitTests.Controllers.Users
{
    public class GetTestApiUserByUsernameTests : UserControllerTestsBase
    {
        [Test]
        public async Task Should_retrieve_user_details()
        {
            const string EMAIL_STEM = DefaultData.FAKE_EMAIL_STEM;
            const int NUMBER = 1;

            var user = new UserBuilder(EMAIL_STEM, NUMBER)
                .WithUserType(UserType.Judge)
                .ForApplication(Application.TestApi)
                .BuildUser();

            QueryHandler
                .Setup(x => x.Handle<GetUserByUsernameQuery, User>(It.IsAny<GetUserByUsernameQuery>()))
                .ReturnsAsync(user);

            var result = await Controller.GetUserDetailsByUsernameAsync(user.Username);

            result.Should().NotBeNull();
            var objectResult = (OkObjectResult) result;
            objectResult.StatusCode.Should().Be((int) HttpStatusCode.OK);

            var userDetails = (UserDetailsResponse) objectResult.Value;
            userDetails.Should().BeEquivalentTo(user);
        }

        [Test]
        public async Task Should_return_not_found_for_unknown_username()
        {
            const string USERNAME = DefaultData.NON_EXISTENT_USERNAME;

            QueryHandler
                .Setup(x => x.Handle<GetUserByUsernameQuery, User>(It.IsAny<GetUserByUsernameQuery>()))
                .ReturnsAsync((User)null);

            var result = await Controller.GetUserDetailsByUsernameAsync(USERNAME);

            result.Should().NotBeNull();
            var objectResult = (NotFoundResult) result;
            objectResult.StatusCode.Should().Be((int) HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_delete_aad_user()
        {
            const string CONTACT_EMAIL = DefaultData.NON_EXISTENT_CONTACT_EMAIL;

            var userProfile = Builder<UserProfile>.CreateNew().Build();
            userProfile.Email = CONTACT_EMAIL;

            UserApiService.Setup(x => x.CheckUserExistsInAAD(CONTACT_EMAIL)).Returns(Task.FromResult(true));
            UserApiService.Setup(x => x.DeleteUserInAAD(CONTACT_EMAIL)).Returns(Task.CompletedTask);

            var result = await Controller.DeleteADUserAsync(CONTACT_EMAIL);

            result.Should().NotBeNull();
            var objectResult = (NoContentResult) result;
            objectResult.StatusCode.Should().Be((int) HttpStatusCode.NoContent);
        }

        [Test]
        public async Task Should_return_not_found_when_deleting_non_existent_aad_user()
        {
            const string CONTACT_EMAIL = DefaultData.NON_EXISTENT_CONTACT_EMAIL;

            UserApiService.Setup(x => x.CheckUserExistsInAAD(CONTACT_EMAIL)).Returns(Task.FromResult(false));

            var result = await Controller.DeleteADUserAsync(CONTACT_EMAIL);

            result.Should().NotBeNull();
            var objectResult = (NotFoundObjectResult) result;
            objectResult.StatusCode.Should().Be((int) HttpStatusCode.NotFound);
        }
    }
}
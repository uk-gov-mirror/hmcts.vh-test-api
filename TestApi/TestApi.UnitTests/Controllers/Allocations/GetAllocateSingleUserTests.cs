﻿using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using TestApi.Contract.Responses;
using TestApi.DAL.Queries;
using TestApi.Domain;
using TestApi.Domain.Enums;

namespace TestApi.UnitTests.Controllers.Allocations
{
    public class GetAllocateSingleUserTests : HearingsControllerTestBase
    {
        [Test]
        public async Task Should_return_allocated_user()
        {
            var user = CreateUser(UserType.CaseAdmin);
            CreateAllocation(user);

            QueryHandler
                .Setup(
                    x => x.Handle<GetAllocatedUserByUserTypeQuery, User>(It.IsAny<GetAllocatedUserByUserTypeQuery>()))
                .ReturnsAsync(user);

            var response = await Controller.AllocateUserByUserTypeAndApplicationAsync(user.UserType, user.Application);
            response.Should().NotBeNull();

            var result = (OkObjectResult) response;
            result.StatusCode.Should().Be((int) HttpStatusCode.OK);

            var userDetailsResponse = (UserDetailsResponse) result.Value;
            userDetailsResponse.Should().BeEquivalentTo(user);
        }
    }
}
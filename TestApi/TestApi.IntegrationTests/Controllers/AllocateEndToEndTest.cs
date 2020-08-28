﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AcceptanceTests.Common.Api.Helpers;
using FluentAssertions;
using NUnit.Framework;
using TestApi.Common.Builders;
using TestApi.Contract.Responses;
using TestApi.Domain.Enums;
using TestApi.IntegrationTests.Controllers.Hearings;
using TestApi.Mappings;
using TestApi.Services.Clients.BookingsApiClient;
using TestApi.Services.Clients.VideoApiClient;
using TestApi.Tests.Common;
using TestApi.Tests.Common.Configuration;

namespace TestApi.IntegrationTests.Controllers
{
    public class AllocateEndToEndTest : HearingsTestsBase
    {
        [Test]
        public async Task Should_create_hearing_and_allocate_users()
        {
            var userTypes = new List<UserType>()
            {
                UserType.Judge,
                UserType.VideoHearingsOfficer,
                UserType.CaseAdmin,
                UserType.Individual,
                UserType.Representative,
                UserType.Individual,
                UserType.Representative
            };

            var allocateRequest = new AllocateUsersRequestBuilder()
                .WithUserTypes(userTypes)
                .ForApplication(Application.TestApi)
                .Build();

            var allocateUsersUri = ApiUriFactory.AllocationEndpoints.AllocateUsers;

            await SendPatchRequest(allocateUsersUri, RequestHelper.Serialise(allocateRequest));

            VerifyResponse(HttpStatusCode.OK, true);
            var usersDetails = RequestHelper.Deserialise<List<UserDetailsResponse>>(Json);

            usersDetails.Should().NotBeNull();
            Verify.UsersDetailsResponse(usersDetails, userTypes);

            var users = UserDetailsResponseToUserMapper.Map(usersDetails);

            var hearingRequest = new HearingBuilder(users)
                .TypeOfTest(TestType.Automated)
                .Build();

            var createHearingUri = ApiUriFactory.HearingEndpoints.CreateHearing;

            await SendPostRequest(createHearingUri, RequestHelper.Serialise(hearingRequest));
            VerifyResponse(HttpStatusCode.Created, true);

            var hearing = RequestHelper.Deserialise<HearingDetailsResponse>(Json);
            hearing.Should().NotBeNull();
            HearingsToDelete.Add(hearing);

            var caseAdmin = hearingRequest.Users.First(x => x.UserType == UserType.CaseAdmin);

            var request = new UpdateBookingStatusRequest()
            {
                Updated_by = caseAdmin.Username,
                AdditionalProperties = null,
                Cancel_reason = null,
                Status = UpdateBookingStatus.Created
            };

            var uri = ApiUriFactory.HearingEndpoints.ConfirmHearing(hearing.Id);
            await SendPatchRequest(uri, RequestHelper.Serialise(request));

            VerifyResponse(HttpStatusCode.Created, true);
            var conference = RequestHelper.Deserialise<ConferenceDetailsResponse>(Json);

            conference.Should().NotBeNull();
            Verify.ConferenceDetailsResponse(conference, hearing);
        }
    }
}
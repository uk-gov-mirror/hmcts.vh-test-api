﻿using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AcceptanceTests.Common.Api.Helpers;
using AcceptanceTests.Common.Api.Uris;
using FluentAssertions;
using NUnit.Framework;
using Polly;
using TestApi.BQSTests.Test;
using TestApi.Common.Data;
using TestApi.Services.Builders.Requests;
using TestApi.Services.Clients.BookingsApiClient;
using TestApi.Services.Clients.VideoApiClient;
using TestApi.Tests.Common;
using TestContext = TestApi.BQSTests.Test.TestContext;

namespace TestApi.BQSTests.Subscriber
{
    public class TestsBase
    {
        // 6 retries ^2 will execute after 2 seconds, then 4, 8, 16, 32 then finally 64 (2 minutes in total)
        protected const int RETRIES = 6;
        protected TestContext Context;
        protected HttpResponseMessage Response;
        protected string Json;
        protected HearingDetailsResponse Hearing;
        protected ConferenceDetailsResponse Conference;
        protected HttpClient Client;

        [OneTimeSetUp]
        public void BeforeTestRun()
        {
            Context = new Setup().RegisterSecrets();
        }

        [SetUp]
        public async Task BeforeEveryTest()
        {
            await CreateAndConfirmHearing();
        }

        [TearDown]
        public async Task AfterEveryTest()
        {
            if (Hearing != null)
            {
                var uri = BookingsApiUriFactory.HearingsEndpoints.RemoveHearing(Hearing.Id);
                await SendDeleteRequest(uri);
            }
        }

        [OneTimeTearDown]
        public void AfterTestRun()
        {
            Client?.Dispose();
        }

        protected void VerifyResponse(HttpStatusCode statusCode, bool isSuccess)
        {
            Response.StatusCode.Should().Be(statusCode);
            Response.IsSuccessStatusCode.Should().Be(isSuccess);
        }

        protected void CreateNewBookingsApiClient(string token)
        {
            Client?.Dispose();
            Client = new HttpClient {BaseAddress = new Uri(Context.Config.Services.BookingsApiUrl)};
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        protected void CreateNewVideoApiClient(string token)
        {
            Client?.Dispose();
            Client = new HttpClient { BaseAddress = new Uri(Context.Config.Services.VideoApiUrl) };
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        protected async Task SendPatchRequest(string uri, string request)
        {
            var content = new StringContent(request, Encoding.UTF8, "application/json");
            CreateNewBookingsApiClient(Context.Tokens.BookingsApiBearerToken);
            var fullUri = new Uri($"{Context.Config.Services.BookingsApiUrl}{uri}");
            Response = await Client.PatchAsync(fullUri, content);
            Json = await Response.Content.ReadAsStringAsync();
        }

        protected async Task SendPostRequest(string uri, string request)
        {
            var content = new StringContent(request, Encoding.UTF8, "application/json");
            CreateNewBookingsApiClient(Context.Tokens.BookingsApiBearerToken);
            var fullUri = new Uri($"{Context.Config.Services.BookingsApiUrl}{uri}");
            Response = await Client.PostAsync(fullUri, content);
            Json = await Response.Content.ReadAsStringAsync();
        }

        protected async Task SendPutRequest(string uri, string request)
        {
            var content = new StringContent(request, Encoding.UTF8, "application/json");
            CreateNewBookingsApiClient(Context.Tokens.BookingsApiBearerToken);
            var fullUri = new Uri($"{Context.Config.Services.BookingsApiUrl}{uri}");
            Response = await Client.PutAsync(fullUri, content);
            Json = await Response.Content.ReadAsStringAsync();
        }

        protected async Task SendDeleteRequest(string uri)
        {
            CreateNewBookingsApiClient(Context.Tokens.BookingsApiBearerToken);
            var fullUri = new Uri($"{Context.Config.Services.BookingsApiUrl}{uri}");
            Response = await Client.DeleteAsync(fullUri);
            Json = await Response.Content.ReadAsStringAsync();
        }

        protected async Task CreateAndConfirmHearing()
        {
            var bookingUri = BookingsApiUriFactory.HearingsEndpoints.BookNewHearing;
            var request = new BookHearingRequestBuilder(Context.Config.UsernameStem).Build();

            await SendPostRequest(bookingUri, RequestHelper.Serialise(request));
            VerifyResponse(HttpStatusCode.Created, true);

            var bookingsResponse = RequestHelper.Deserialise<HearingDetailsResponse>(Json);
            bookingsResponse.Should().NotBeNull();
            Hearing = bookingsResponse;

            var confirmRequest = new UpdateBookingStatusRequest()
            {
                Status = UpdateBookingStatus.Created,
                Updated_by = HearingData.CREATED_BY(Context.Config.UsernameStem)
            };

            var updateUri = BookingsApiUriFactory.HearingsEndpoints.UpdateHearingStatus(Hearing.Id);
            await SendPatchRequest(updateUri, RequestHelper.Serialise(confirmRequest));

            var response = await GetConferenceByHearingIdPollingAsync(Hearing.Id);
            response.Should().NotBeNull();
            Verify.ConferenceDetailsResponse(response, Hearing);
            Conference = response;
        }

        private async Task<ConferenceDetailsResponse> GetConferenceByHearingIdPollingAsync(Guid hearingRefId)
        {
            var uri = $"{Context.Config.Services.VideoApiUrl}{VideoApiUriFactory.ConferenceEndpoints.GetConferenceByHearingRefId(hearingRefId)}";
            CreateNewVideoApiClient(Context.Tokens.VideoApiBearerToken);

            var policy = Policy
                .HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                .WaitAndRetryAsync(RETRIES, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            try
            {
                var result = await policy.ExecuteAsync(async () => await Client.GetAsync(uri));
                var conferenceResponse = RequestHelper.Deserialise<ConferenceDetailsResponse>(await result.Content.ReadAsStringAsync());
                conferenceResponse.Case_name.Should().NotBeNullOrWhiteSpace();
                return conferenceResponse;
            }
            catch (Exception e)
            {
                throw new Exception($"Encountered error '{e.Message}' after {RETRIES ^ 2} seconds.");
            }
        }
    }
}
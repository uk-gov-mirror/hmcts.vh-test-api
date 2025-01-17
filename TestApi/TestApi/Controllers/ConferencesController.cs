﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSwag.Annotations;
using VideoApi.Client;
using VideoApi.Contract.Requests;
using VideoApi.Contract.Responses;

namespace TestApi.Controllers
{
    [Consumes("application/json")]
    [Produces("application/json")]
    [Route("conferences")]
    [ApiController]
    public class ConferencesController : ControllerBase
    {
        private readonly ILogger<ConferencesController> _logger;
        private readonly IVideoApiClient _videoApiClient;

        public ConferencesController(ILogger<ConferencesController> logger, IVideoApiClient videoApiClient)
        {
            _logger = logger;
            _videoApiClient = videoApiClient;
        }

        /// <summary>
        ///     Get the details of a conference by id
        /// </summary>
        /// <param name="conferenceId">Id of the conference</param>
        /// <returns>Full details of a conference</returns>
        [HttpGet("{conferenceId}", Name = nameof(GetConferenceById))]
        [OpenApiOperation("GetConferenceById")]
        [ProducesResponseType(typeof(ConferenceDetailsResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetConferenceById(Guid conferenceId)
        {
            _logger.LogDebug("GetConferenceById {conferenceId}", conferenceId);

            try
            {
                var response = await _videoApiClient.GetConferenceDetailsByIdAsync(conferenceId);
                return Ok(response);
            }
            catch (VideoApiException e)
            {
                return StatusCode(e.StatusCode, e.Response);
            }
        }

        /// <summary>
        ///     Get the details of a conference by hearing ref id
        /// </summary>
        /// <param name="hearingRefId">Hearing ref Id of the conference</param>
        /// <returns>Full details of a conference</returns>
        [HttpGet("hearings/{hearingRefId}", Name = nameof(GetConferenceByHearingRefId))]
        [OpenApiOperation("GetConferenceByHearingRefId")]
        [ProducesResponseType(typeof(ConferenceDetailsResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetConferenceByHearingRefId(Guid hearingRefId)
        {
            _logger.LogDebug("GetConferenceByHearingRefId {hearingRefId}", hearingRefId);

            try
            {
                var response = await _videoApiClient.GetConferenceByHearingRefIdAsync(hearingRefId, false);
                return Ok(response);
            }
            catch (VideoApiException e)
            {
                return StatusCode(e.StatusCode, e.Response);
            }
        }

        /// <summary>
        /// Request to book a conference
        /// </summary>
        /// <param name="request">Details of a conference</param>
        /// <returns>Details of the new conference</returns>
        [HttpPost]
        [OpenApiOperation("BookNewConference")]
        [ProducesResponseType(typeof(ConferenceDetailsResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> BookNewConference(BookNewConferenceRequest request)
        {
            _logger.LogDebug($"BookNewConference");

            try
            {
                var response = await _videoApiClient.BookNewConferenceAsync(request);
                return Created(nameof(BookNewConference), response);
            }
            catch (VideoApiException e)
            {
                return StatusCode(e.StatusCode, e.Response);
            }
        }

        /// <summary>
        ///     Delete a conference by conference id
        /// </summary>
        /// <param name="hearingRefId">Hearing Ref Id of the conference</param>
        /// <param name="conferenceId">Conference Id of the conference</param>
        /// <returns></returns>
        [HttpDelete("{hearingRefId}/{conferenceId}")]
        [OpenApiOperation("DeleteConference")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DeleteConference(Guid hearingRefId, Guid conferenceId)
        {
            _logger.LogDebug("DeleteConference {conferenceId}", conferenceId);

            try
            {
                await _videoApiClient.RemoveConferenceAsync(conferenceId);
            }
            catch (VideoApiException e)
            {
                return StatusCode(e.StatusCode, e.Response);
            }

            try
            {
                await _videoApiClient.DeleteAudioApplicationAsync(hearingRefId);

                _logger.LogInformation("Successfully deleted audio application with hearing id {hearingRefId}", hearingRefId);
            }
            catch (VideoApiException e)
            {
                if (e.StatusCode != (int)HttpStatusCode.NotFound) return StatusCode(e.StatusCode, e.Response);

                _logger.LogInformation("No audio application found to delete with hearing id {hearingRefId}", hearingRefId);
            }

            return NoContent();
        }

        /// <summary>
        ///     Get conferences for today Judge
        /// </summary>
        /// <param name="username">Username of the Judge</param>
        /// <returns>Full details of all conferences</returns>
        [HttpGet("today/judge", Name = nameof(GetConferencesForTodayJudge))]
        [OpenApiOperation("GetConferencesForTodayJudge")]
        [ProducesResponseType(typeof(List<ConferenceForJudgeResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetConferencesForTodayJudge(string username)
        {
            _logger.LogDebug("GetConferencesForTodayJudge {username}", username);

            try
            {
                var response = await _videoApiClient.GetConferencesTodayForJudgeByUsernameAsync(username);
                return Ok(response);
            }
            catch (VideoApiException e)
            {
                return StatusCode(e.StatusCode, e.Response);
            }
        }

        /// <summary>
        ///     Get conferences for today VHO
        /// </summary>
        /// <returns>Full details of all conferences</returns>
        [HttpGet("today/vho", Name = nameof(GetConferencesForTodayVho))]
        [OpenApiOperation("GetConferencesForTodayVho")]
        [ProducesResponseType(typeof(List<ConferenceForAdminResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetConferencesForTodayVho()
        {
            _logger.LogDebug($"GetConferencesForTodayVho");

            try
            {
                var response = await _videoApiClient.GetConferencesTodayForAdminAsync(new List<string>());
                return Ok(response);
            }
            catch (VideoApiException e)
            {
                return StatusCode(e.StatusCode, e.Response);
            }
        }

        /// <summary>
        ///     Get audio recording links
        /// </summary>
        /// <param name="hearingId">Hearing Id of the conference</param>
        /// <returns>A list of audio recording links for a conference</returns>
        [HttpGet("audio/{hearingId}", Name = nameof(GetAudioRecordingLinksByHearingId))]
        [OpenApiOperation("GetAudioRecordingLinksByHearingId")]
        [ProducesResponseType(typeof(AudioRecordingResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAudioRecordingLinksByHearingId(Guid hearingId)
        {
            _logger.LogDebug("GetAudioRecordingLinksByHearingId {hearingId}", hearingId);

            try
            {
                var response = await _videoApiClient.GetAudioRecordingLinkAsync(hearingId);
                return Ok(response);
            }
            catch (VideoApiException e)
            {
                return StatusCode(e.StatusCode, e.Response);
            }
        }

        /// <summary>
        ///     Get tasks for a conference
        /// </summary>
        /// <param name="conferenceId">Conference Id of the conference</param>
        /// <returns>A list of task details for a conference</returns>
        [HttpGet("tasks/{conferenceId}", Name = nameof(GetTasksByConferenceId))]
        [OpenApiOperation("GetTasksByConferenceId")]
        [ProducesResponseType(typeof(List<TaskResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetTasksByConferenceId(Guid conferenceId)
        {
            _logger.LogDebug("GetTasksByConferenceId {conferenceId}", conferenceId);

            try
            {
                var response = await _videoApiClient.GetTasksForConferenceAsync(conferenceId);
                return Ok(response);
            }
            catch (VideoApiException e)
            {
                return StatusCode(e.StatusCode, e.Response);
            }
        }

        /// <summary>
        ///     Create event
        /// </summary>
        /// <param name="request">Conference event request</param>
        /// <returns></returns>
        [HttpPost("events", Name = nameof(CreateEvent))]
        [OpenApiOperation("CreateEvent")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateEvent(ConferenceEventRequest request)
        {
            _logger.LogDebug($"CreateEvent");

            try
            {
                await _videoApiClient.RaiseVideoEventAsync(request);
                return NoContent();
            }
            catch (VideoApiException e)
            {
                return StatusCode(e.StatusCode, e.Response);
            }
        }

        /// <summary>
        ///     Get the test call result for a participant
        /// </summary>
        /// <param name="conferenceId">Conference Id of the conference</param>
        /// <param name="participantId">Participant Id of the participant</param>
        /// <returns>Self test score</returns>
        [HttpGet("{conferenceId}/participants/{participantId}/score", Name = nameof(GetSelfTestScore))]
        [OpenApiOperation("GetSelfTestScore")]
        [ProducesResponseType(typeof(TestCallScoreResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetSelfTestScore(Guid conferenceId, Guid participantId)
        {
            _logger.LogDebug("GetSelfTestScore {conferenceId} {participantId}", conferenceId, participantId);

            try
            {
                var response = await _videoApiClient.GetTestCallResultForParticipantAsync(conferenceId, participantId);
                return Ok(response);
            }
            catch (VideoApiException e)
            {
                return StatusCode(e.StatusCode, e.Response);
            }
        }

        /// <summary>
        ///     Delete a participant
        /// </summary>
        /// <param name="conferenceId">Conference Id of the conference</param>
        /// <param name="participantId">Participant Id of the participant</param>
        /// <returns></returns>/returns>
        [HttpDelete("{conferenceId}/participants/{participantId}", Name = nameof(DeleteParticipant))]
        [OpenApiOperation("DeleteParticipant")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DeleteParticipant(Guid conferenceId, Guid participantId)
        {
            _logger.LogDebug("DeleteParticipant {conferenceId} {participantId}", conferenceId, participantId);

            try
            {
                await _videoApiClient.RemoveParticipantFromConferenceAsync(conferenceId, participantId);
                return NoContent();
            }
            catch (VideoApiException e)
            {
                return StatusCode(e.StatusCode, e.Response);
            }
        }
    }
}

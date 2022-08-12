﻿using AspNetCore.SignalR.EventStream.Authorization;
using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AspNetCore.SignalR.EventStream.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ServiceFilter(typeof(EventStreamAuthorizeAttribute))]
    public class EventStreamController : ControllerBase
    {
        private readonly IEventStreamService _streamsService;
        private readonly ILogger<EventStreamLog>? _logger;

        public EventStreamController(IEventStreamService streamsService, ILogger<EventStreamLog>? logger = null)
        {
            _streamsService = streamsService;
            _logger = logger;
        }

        [HttpPost("streams/associate")]
        public async Task<IActionResult> AssociateStreams([FromBody] AssociateStreamsModel associateStreamsModel)
        {
            _logger?.LogInformation($"Associating streams to stream {associateStreamsModel.Name ?? associateStreamsModel.StreamId.ToString()}");
            var mergedStream = await _streamsService.AssociateStreamsAsync(associateStreamsModel);
            _logger?.LogInformation($"Finished associating streams to stream {associateStreamsModel.Name ?? associateStreamsModel.StreamId.ToString()}");

            return Ok(mergedStream);
        }

        [HttpPost("streams/search")]
        public IActionResult SearchStreams([FromBody] SearchStreamsModel searchStreamsModel)
        {
            _logger?.LogInformation($"Searching streams : {JsonSerializer.Serialize(searchStreamsModel)}");

            var streams = _streamsService.SearchStreamsAsync(searchStreamsModel);

            _logger?.LogInformation($"Finished searching streams : {JsonSerializer.Serialize(searchStreamsModel)}");

            return Ok(streams);
        }

        [HttpDelete("streams/{id}")]
        public async Task<IActionResult> DeleteStream(long id)
        {
            _logger?.LogInformation($"Deleting stream {id}");

            await _streamsService.DeleteEventStreamAsync(id);

            _logger?.LogInformation($"Finished deleting stream {id}");

            return Ok();
        }

        [HttpGet("events/{id}")]
        public async Task<IActionResult> GetEvent(Guid id)
        {
            _logger?.LogInformation($"Fetching event {id}");

            var @event = await _streamsService.GetEventAsync(id);

            _logger?.LogInformation($"Finished fetching event {id}");

            return Ok(@event);
        }

        [HttpGet("subscribers/{id}")]
        public async Task<IActionResult> GetSubscriber(Guid id)
        {
            _logger?.LogInformation($"Fetching subscriber {id}");

            var subscriber = await _streamsService.GetSubscriberAsync(id);

            _logger?.LogInformation($"Finished fetching subscriber {id}");

            return Ok(subscriber);  
        }

        [HttpPut("subscribers/{subscriberId:Guid}")]
        public async Task<IActionResult> UpdateSubscriber(Guid subscriberId, [FromBody] UpdateSubscriberModel model)
        {
            _logger?.LogInformation($"Updating subscriber {subscriberId}");

            await _streamsService.UpdateSubscriberAsync(subscriberId, model);

            _logger?.LogInformation($"Finished updating subscriber {subscriberId}");

            return Ok();
        }
    }
}

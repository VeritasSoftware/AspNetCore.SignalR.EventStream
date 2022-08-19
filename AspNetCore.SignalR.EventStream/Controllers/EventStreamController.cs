using AspNetCore.SignalR.EventStream.Authorization;
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
        private readonly ILogger<EventStreamController>? _logger;

        public EventStreamController(IEventStreamService streamsService, ILogger<EventStreamController>? logger = null)
        {
            _streamsService = streamsService;
            _logger = logger;
        }

        [HttpGet("streams/{id}")]
        public async Task<IActionResult> GetStream(Guid id)
        {
            try
            {
                _logger?.LogInformation($"Fetching stream {id}");

                var stream = await _streamsService.GetStreamAsync(id);

                _logger?.LogInformation($"Finished fetching stream {id}");

                return Ok(stream);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{nameof(EventStreamController)} error.");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }            
        }

        [HttpGet("streams/stream/{name}")]
        public async Task<IActionResult> GetStream(string name)
        {
            try
            {
                _logger?.LogInformation($"Fetching stream {name}");

                var stream = await _streamsService.GetStreamAsync(name);

                _logger?.LogInformation($"Finished fetching stream {name}");

                return Ok(stream);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{nameof(EventStreamController)} error.");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }            
        }

        [HttpGet("events/{streamId}/{id}")]
        public async Task<IActionResult> GetEvent(long streamId, Guid id)
        {
            try
            {
                _logger?.LogInformation($"Fetching event {id}");

                var @event = await _streamsService.GetEventAsync(streamId, id);

                _logger?.LogInformation($"Finished fetching event {id}");

                return Ok(@event);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{nameof(EventStreamController)} error.");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }            
        }

        [HttpGet("subscribers/{id}")]
        public async Task<IActionResult> GetSubscriber(Guid id)
        {
            try
            {
                _logger?.LogInformation($"Fetching subscriber {id}");

                var subscriber = await _streamsService.GetSubscriberAsync(id);

                _logger?.LogInformation($"Finished fetching subscriber {id}");

                return Ok(subscriber);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{nameof(EventStreamController)} error.");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }            
        }

        [HttpPost("streams/associate")]
        public async Task<IActionResult> AssociateStreams([FromBody] AssociateStreamsModel associateStreamsModel)
        {
            try
            {
                _logger?.LogInformation($"Associating streams to stream {associateStreamsModel.Name ?? associateStreamsModel.StreamId.ToString()}");
                var mergedStream = await _streamsService.AssociateStreamsAsync(associateStreamsModel);
                _logger?.LogInformation($"Finished associating streams to stream {associateStreamsModel.Name ?? associateStreamsModel.StreamId.ToString()}");

                return Ok(mergedStream);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{nameof(EventStreamController)} error.");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }            
        }

        [HttpPost("streams/search")]
        public IActionResult SearchStreams([FromBody] SearchStreamsModel searchStreamsModel)
        {
            try
            {
                _logger?.LogInformation($"Searching streams : {JsonSerializer.Serialize(searchStreamsModel)}");

                var streams = _streamsService.SearchStreamsAsync(searchStreamsModel);

                _logger?.LogInformation($"Finished searching streams : {JsonSerializer.Serialize(searchStreamsModel)}");

                return Ok(streams);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{nameof(EventStreamController)} error.");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }            
        }

        [HttpPut("subscribers/{id}")]
        public async Task<IActionResult> UpdateSubscriber(Guid id, [FromBody] UpdateSubscriberModel model)
        {
            try
            {
                _logger?.LogInformation($"Updating subscriber {id}");

                await _streamsService.UpdateSubscriberAsync(id, model);

                _logger?.LogInformation($"Finished updating subscriber {id}");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{nameof(EventStreamController)} error.");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }            
        }

        [HttpDelete("streams/{id}")]
        public async Task<IActionResult> DeleteStream(long id)
        {
            try
            {
                _logger?.LogInformation($"Deleting stream {id}");

                await _streamsService.DeleteEventStreamAsync(id);

                _logger?.LogInformation($"Finished deleting stream {id}");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{nameof(EventStreamController)} error.");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }            
        }
    }
}

using AspNetCore.SignalR.EventStream.Authorization;
using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.SignalR.EventStream.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ServiceFilter(typeof(EventStreamAuthorizeAttribute))]
    public class EventStreamController : ControllerBase
    {
        private readonly IEventStreamService _streamsService;

        public EventStreamController(IEventStreamService streamsService)
        {
            _streamsService = streamsService;
        }

        [HttpPost("streams/associate")]
        public async Task<IActionResult> AssociateStreams([FromBody] AssociateStreamsModel associateStreamsModel)
        {
            var mergedStream = await _streamsService.AssociateStreamsAsync(associateStreamsModel);

            return Ok(mergedStream);
        }

        [HttpPost("streams/search")]
        public IActionResult SearchStreams([FromBody] SearchStreamsModel searchStreamsModel)
        {
            var streams = _streamsService.SearchStreamsAsync(searchStreamsModel);

            return Ok(streams);
        }

        [HttpDelete("streams/{id}")]
        public async Task<IActionResult> DeleteStream(long id)
        {
            await _streamsService.DeleteEventStreamAsync(id);

            return Ok();
        }

        [HttpGet("events/{id}")]
        public async Task<IActionResult> GetEvent(Guid id)
        {
            var @event = await _streamsService.GetEventAsync(id);

            return Ok(@event);
        }

        [HttpGet("subscribers/{id}")]
        public async Task<IActionResult> GetSubscriber(Guid id)
        {
            var subscriber = await _streamsService.GetSubscriberAsync(id);

            return Ok(subscriber);
        }

        [HttpPut("subscribers")]
        public async Task<IActionResult> UpdateSubscriber(Guid subscriberId, [FromBody] UpdateSubscriberModel model)
        {
            await _streamsService.UpdateSubscriberAsync(subscriberId, model);

            return Ok();
        }
    }
}

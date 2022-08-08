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
            var mergedStream = await _streamsService.MergeStreams(associateStreamsModel);

            return Ok(mergedStream);
        }

        [HttpPost("streams/search")]
        public async Task<IActionResult> SearchStreams([FromBody] SearchStreamsModel searchStreamsModel)
        {
            var streams = await _streamsService.SearchStreams(searchStreamsModel);

            return Ok(streams);
        }

        [HttpDelete("streams/{id}")]
        public async Task<IActionResult> DeleteStream(long id)
        {
            await _streamsService.DeleteEventStreamAsync(id);

            return Ok();
        }
    }
}

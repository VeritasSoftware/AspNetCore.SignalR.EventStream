using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.SignalR.EventStream.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventStreamController : ControllerBase
    {
        private readonly IEventStreamService _streamsService;

        public EventStreamController(IEventStreamService streamsService)
        {
            _streamsService = streamsService;
        }

        [HttpPost("associate")]
        public async Task<IActionResult> AssociateStreams([FromBody] AssociateStreamsModel mergeStreamsModel)
        {
            var mergedStream = await _streamsService.MergeStreams(mergeStreamsModel);

            return Ok(mergedStream);
        }
    }
}

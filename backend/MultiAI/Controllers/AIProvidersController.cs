using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiAI.Models.Request;
using MultiAI.Services.Interface;

namespace MultiAI.Controllers
{
    [Route("api/providers")]
    [ApiController]
    public class AIProvidersController : ControllerBase
    {
        private readonly IAIProviderFactory providerFactory;

        public AIProvidersController(IAIProviderFactory providerFactory)
        {
            this.providerFactory = providerFactory;
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateApiKey([FromBody] ValidateApiKeyRequest request)
        {
            var provider = providerFactory.GetProvider(request.Provider);

            var valid = await provider.ValidateKey(request.ApiKey);

            return Ok(new {Sucess=valid});
        }

        [HttpPost("models")]
        public async Task<IActionResult> GetModels([FromBody] GetModelsRequest request)
        {
            var provider = providerFactory.GetProvider(request.Provider);

            var models = await provider.GetModels(request.ApiKey);

            return Ok(models);
        }
    }
}

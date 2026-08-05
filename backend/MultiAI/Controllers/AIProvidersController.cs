using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.Request;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Controllers
{
    [Route("api/providers")]
    [ApiController]
    [Authorize]
    public class AIProvidersController : ControllerBase
    {
        private readonly IAIProviderFactory providerFactory;
        private readonly AppDbContext dbContext;
        private readonly IUserAIProviderService userAIProviderService;

        public AIProvidersController(
            IAIProviderFactory providerFactory,
            AppDbContext dbContext,
            IUserAIProviderService userAIProviderService)
        {
            this.providerFactory = providerFactory;
            this.dbContext = dbContext;
            this.userAIProviderService = userAIProviderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProviders()
        {
            var providers = await dbContext.AIProviders
                .AsNoTracking()
                .Where(x => x.IsActive && x.IsDeleted == 0)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.SupportsModelListing,
                    x.IsActive
                })
                .ToListAsync();

            return Ok(providers);
        }

        [HttpGet("connected")]
        public async Task<IActionResult> GetConnected()
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            var providers = await userAIProviderService.GetConnectedAsync(userId);
            return Ok(providers.Select(x => new
            {
                x.Id,
                x.ProviderId,
                ProviderName = x.Provider.Name,
                x.IsDefault,
                x.DefaultModel,
                SupportsModelListing = x.Provider.SupportsModelListing
            }));
        }

        /// <summary>
        /// Lists models for a connected provider using the stored API key.
        /// </summary>
        [HttpGet("connected/{id:int}/models")]
        public async Task<IActionResult> GetConnectedModels(int id)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                var models = await userAIProviderService.GetModelsAsync(userId, id);
                return Ok(models);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Sets the default model for a connected provider.
        /// </summary>
        [HttpPut("connected/{id:int}/default-model")]
        public async Task<IActionResult> SetDefaultModel(int id, [FromBody] SetDefaultModelRequest request)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                var connected = await userAIProviderService.SetDefaultModelAsync(userId, id, request.DefaultModel);
                return Ok(new
                {
                    connected.Id,
                    connected.ProviderId,
                    ProviderName = connected.Provider.Name,
                    connected.IsDefault,
                    connected.DefaultModel,
                    SupportsModelListing = connected.Provider.SupportsModelListing
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Marks a connected provider as the user's default provider.
        /// </summary>
        [HttpPut("connected/{id:int}/set-default")]
        public async Task<IActionResult> SetDefaultProvider(int id)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                var connected = await userAIProviderService.SetDefaultProviderAsync(userId, id);
                return Ok(new
                {
                    connected.Id,
                    connected.ProviderId,
                    ProviderName = connected.Provider.Name,
                    connected.IsDefault,
                    connected.DefaultModel,
                    SupportsModelListing = connected.Provider.SupportsModelListing
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody] SaveApiKeyRequest request)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                var connected = await userAIProviderService.ConnectAsync(
                    userId,
                    request.ProviderId,
                    request.ApiKey);

                return Ok(new
                {
                    connected.Id,
                    connected.ProviderId,
                    connected.IsDefault,
                    connected.DefaultModel
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateApiKey([FromBody] ValidateApiKeyRequest request)
        {
            var provider = providerFactory.GetProvider(request.Provider);
            var valid = await provider.ValidateKey(request.ApiKey);
            return Ok(new { Sucess = valid });
        }

        [HttpPost("models")]
        public async Task<IActionResult> GetModels([FromBody] GetModelsRequest request)
        {
            var provider = providerFactory.GetProvider(request.Provider);
            var models = await provider.GetModels(request.ApiKey);
            return Ok(models);
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

using Microsoft.AspNetCore.Mvc;
using P3.FundamentalData.API.Repository.IRepository;
using P3.FundamentalData.API.Services;

namespace P3.FundamentalData.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class tblApiController : ControllerBase
	{
		private readonly IConfiguration _configuration;
		private readonly ApiConnection _apiConnection;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IUnitOfWork _unitOfWork;

		public tblApiController(IUnitOfWork unitOfWork, IHttpClientFactory httpClientFactory, IConfiguration configuration, ApiConnection apiConnection)
		{
			_unitOfWork = unitOfWork;
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
			_apiConnection = apiConnection;
		}
		//Get next download date from tblApi
		[HttpGet("getNextDownloadDate/{ApiName}")]
		public async Task<IActionResult> GetNextDownloadFromTblApi(string ApiName)
		{
			try
			{
				//var paramValue = "FundHoldingsInstitutionalHolder";
				var result = await _unitOfWork.tblApiData.GetFirstOrDefault(x => x.ApiName == ApiName);
				string nextDownloadDate = result.NextDownload?.ToString("yyyy-MM-dd");
				if (nextDownloadDate == null)
				{
					return Ok("Null");
				}
				//DateTime today = DateTime.Now;
				string today = DateTime.Now.ToString("yyyy-MM-dd");
				int comparisonResult = nextDownloadDate.CompareTo(today);

				if (comparisonResult < 1)
				{
					return Ok("DatePassed");
				}
				return Ok(nextDownloadDate);
			}
			catch (Exception ex)
			{
				return BadRequest(new
				{
					code = "400",
					Message = "An error occurred while working on TblApi"
				});
			}
		}
		//Update the next downlaod date
		[HttpGet("updateNextDownloadDate/{ApiName}/{Duration=Annual}")]
		public async Task<IActionResult> UpdateNextDownloadInTblApi(string ApiName, string Duration)
		{
			try
			{
				//var paramValue = "FundHoldingsInstitutionalHolder";
				string tblApiQuery = $"Exec updateTblApi '{ApiName}', '{Duration}'";
				await _unitOfWork.tblApiData.ExecuteSQLProcedureAsync(tblApiQuery);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest(new
				{
					code = "400",
					Message = "An error occurred while updating TblApi"
				});
			}
		}

		//Get if the API is active or inactive
		[HttpGet("getInActiveStatus/{ApiName}")]
		public async Task<IActionResult> GetInActiveStatusFromTblApi(string ApiName)
		{
			try
			{
				//var paramValue = "FundHoldingsInstitutionalHolder";
				var result = await _unitOfWork.tblApiData.GetFirstOrDefault(x => x.ApiName == ApiName);
				string InActiveStatus = result.IsInactive?.ToString();
				if (InActiveStatus == null)
				{
					return Ok("Null");
				}
				else if (InActiveStatus == "False")
				{
					return Ok("False");
				}
				return Ok("True");
			}
			catch (Exception ex)
			{
				return BadRequest(new
				{
					code = "400",
					Message = "An error occurred while working on TblApi"
				});
			}
		}
	}
}

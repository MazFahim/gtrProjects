using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using P3.FundamentalData.Hangfire.Models;
//using P3.FundamentalData.API.Repository.IRepository;
using P3.FundamentalData.Hangfire.Models.DTO;
using P3.FundamentalData.Hangfire.Services;

namespace P3.FundamentalData.Hangfire.Controllers
{
	public class CategoryWiseJob : Controller
	{
		private readonly IHttpClientFactory _httpClientFactory;
		public readonly ApiConnection _apiConnection;
		public readonly JobStorage _jobstorage;
		private readonly IMemoryCache _memoryCache;
		//private readonly IUnitOfWork _unitOfWork;

		public CategoryWiseJob(ApiConnection apiConnection, JobStorage jobStorage, IMemoryCache memoryCache)//, IUnitOfWork unitOfWork)
		{
			_apiConnection = apiConnection;
			_jobstorage = jobStorage;
			_memoryCache = memoryCache;
			//_unitOfWork = unitOfWork;
		}
		public IActionResult Index(string message=null)
		{
			ViewBag.Cache = message;
			return View();
		}

		//Get symbols from Api and save in memory cache.
		public async Task<IActionResult> GetOrSetSymbols()
		{
			var symbols = await GetOrSetSymbolsInCache();
			if (symbols != null)
			{
				Console.WriteLine(symbols);
				var routeValues = new RouteValueDictionary {
				  { "message", "Symbol list has cached."}
				};
				return RedirectToAction("Index", "CategoryWiseJob", routeValues);

			}
			return RedirectToAction("Index","CategoryWiseJob");
		}
		public async Task<List<string>> GetOrSetSymbolsInCache()
		{
			try
			{
				if (_memoryCache.TryGetValue("symbols", out string cachedData))
				{
					List<string> symbolList = JsonConvert.DeserializeObject<List<string>>(cachedData);
					return symbolList; // Return data from cache if it exists
				}


				using var client = _apiConnection.CreateHttpClient();
				var response = await client.GetAsync($"/api/symbol/list-of-symbols");
				if (response.IsSuccessStatusCode)
				{
					var symbols = await response.Content.ReadAsStringAsync();
					//TimeSpan cacheDuration = TimeSpan.FromMinutes(5);
					_memoryCache.Set("symbols", symbols);
					List<string> symbolList = JsonConvert.DeserializeObject<List<string>>(symbols);
					return symbolList;
				}
				return new List<string>();
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
	
		}

		// Delete jobs from Hangfire database.
		public async Task<IActionResult> DeleteHangfireJobs()
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/symbol/DeleteFundamentalJob");
			return RedirectToAction("Index", "CategoryWiseJob");
		}
		//Recurring Job Snippet Start
		public IActionResult ScheduleRecurringJobs()
		{
			string cronExpressionDaily = "30 09 * * *";
			string cronExpressionWeekly = "00 18 * * 6";
			//RecurringJob.AddOrUpdate(() => FireJobCategoryWise(), cronExpression);
			TimeZoneInfo bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka");

			var options = new RecurringJobOptions
			{
				//TimeZone = TimeZoneInfo.Utc // Time zone for the job schedule
				TimeZone = bdTimeZone                          // You can add more options here if needed
			};
			RecurringJob.AddOrUpdate(() => DailyFundamental(), cronExpressionDaily, options);
			RecurringJob.AddOrUpdate(() => WeeklyFundamental(), cronExpressionWeekly, options);

			return RedirectToAction("Index", "CategoryWiseJob");
		}

		public async Task<IActionResult> DailyFundamental()
		{
			var dayOfWeek = DateTime.Today.DayOfWeek.ToString();
			if (dayOfWeek != "Sunday" && dayOfWeek != "Monday")
			{
				BackgroundJob.Enqueue(() => CreateBatchRequestJob());
				BackgroundJob.Enqueue(() => CreateSharesFloatJob());
				BackgroundJob.Enqueue(() => CreateCompanyProfileJob());
				BackgroundJob.Enqueue(() => CreateCompanyInformationMarketCapitalizationJob());
				BackgroundJob.Enqueue(() => CreateRSIDataJob());
			}
			return RedirectToAction("Index", "CategoryWiseJob");
		}
		public async Task<IActionResult> WeeklyFundamental()
		{
			try
			{
				BackgroundJob.Enqueue(() => CreateStockStatisticsAnalystGradeJob());
				BackgroundJob.Enqueue(() => CreateStockStatisticsEarnignSurprisesJob()); //Job Expires
				BackgroundJob.Enqueue(() => CreateStockStatisticsAnalystEstimateAnnualyJob());
				BackgroundJob.Enqueue(() => CreateStockStatisticsAnalystEstimateQuarterlyJob());
				BackgroundJob.Enqueue(() => CreateStockListSymbolsListJob());
				BackgroundJob.Enqueue(() => CreateStockListAvailableListJob());
				BackgroundJob.Enqueue(() => CreateStockListETFListJob());
				BackgroundJob.Enqueue(() => CreateMarketIndexesMajorIndexJob());
				BackgroundJob.Enqueue(() => CreateMarketIndexesCompanyListOfSP500Job());
				BackgroundJob.Enqueue(() => CreateMarketIndexesHistoricalSP500Job());
				BackgroundJob.Enqueue(() => CreateMarketIndexesNasdaq100companiesJob());
				BackgroundJob.Enqueue(() => CreateInsiderTradingtransactiontypesJob());
				BackgroundJob.Enqueue(() => CreateInsiderTradingSpecificSymbolJob());
				BackgroundJob.Enqueue(() => CreateStockFundamentalsAnnuallyIncomeStatementAPIJob());
				BackgroundJob.Enqueue(() => CreateStockFundamentalsQuarterlyIncomeStatementAPIJob());
				BackgroundJob.Enqueue(() => CreateStockFundamentalsAnnuallyBalanceSheetAPIJob());
				BackgroundJob.Enqueue(() => CreateStockFundamentalsQuarterlyBalanceSheetAPIJob());
				BackgroundJob.Enqueue(() => CreateStockFundamentalsAnnuallyCashFlowSheetAPIJob());
				BackgroundJob.Enqueue(() => CreateStockFundamentalsQuarterlyCashFlowSheetAPIJob());

				BackgroundJob.Enqueue(() => CreateStockFundamentalsSECFilings());
				BackgroundJob.Enqueue(() => CreateFundHoldingsETFHoldersJob());
				BackgroundJob.Enqueue(() => CreateFundHoldingsInstitutionalHolderJob());
				BackgroundJob.Enqueue(() => CreateFundHoldingsMutualFundHolderJob());
				BackgroundJob.Enqueue(() => CreateFundHoldingsETFSectorWeightingsJob());
				BackgroundJob.Enqueue(() => CreateFundHoldingsETFCountryWeightingsJob());
				BackgroundJob.Enqueue(() => CreateFundHoldingsETFStockExposureListJob());
				BackgroundJob.Enqueue(() => CreateEconomicsMarketRiskJob());

				BackgroundJob.Enqueue(() => CreateAnnualCompanyFinancialRatioJob());
				BackgroundJob.Enqueue(() => CreateQuarterCompanyFinancialRatioJob());
				BackgroundJob.Enqueue(() => CreateCompanyFinancialRatioTTMJob());
				BackgroundJob.Enqueue(() => CreateAnnualCompanyEnterpriseValueJob());
				BackgroundJob.Enqueue(() => CreatequarterCompanyEnterpriseValueJob());
				BackgroundJob.Enqueue(() => CreateIncomeStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateCashFlowStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateCompanyKeyMetricsTTMStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateAnnualCompanyKeyMetricsStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateQuarterCompanyKeyMetricsStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateAnnualCompanyFinancialGrowthStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateQuarterCompanyFinancialGrowthStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateHistoricCompanyRatingStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateDailyCompanyRatingStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateAnnualCompaniesHistoricalDCFStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateQuarterlyCompaniesHistoricalDCFStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateDaillyCompaniesHistoricalDCFStatementGrowthJob());
				BackgroundJob.Enqueue(() => CreateCompaniesDCFJob());
				BackgroundJob.Enqueue(() => CreateAdvanceDCFprojectionIncludingWACCJob());
				BackgroundJob.Enqueue(() => CreateAdvanceLeveredDCFprojectionIncludingWACCJob());
				BackgroundJob.Enqueue(() => CreateEarningsCalendarJob());
				
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the CRON job");
			}
			
			return RedirectToAction("Index", "CategoryWiseJob");
		}
		// Batch Request Api Controller Start
		public async Task<IActionResult> CreateBatchRequestJob()
		{
			var symbols = await GetOrSetSymbolsInCache();
			var newSymbol = await ConvertAndCombine(symbols, 10);
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "BatchRequestBatchDataAPI";
				string duration = "Daily";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						foreach (var symbol in newSymbol)
						{
							BackgroundJob.Enqueue(() => BatchRequestBatchDataAPI(symbol));
							//break;
						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task BatchRequestBatchDataAPI(string symbol)
		{
			
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/batchrequest/batchdata/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}

		}
		public async Task<List<string>> ConvertAndCombine(List<string> inputList, int chunkSize)
		{
			if (inputList == null || inputList.Count == 0)
			{
				return new List<string>();
			}

			var result = new List<string>();
			var currentChunk = new List<string>();

			foreach (var item in inputList)
			{
				currentChunk.Add(item);

				if (currentChunk.Count == chunkSize)
				{
					result.Add(string.Join(",", currentChunk));
					currentChunk.Clear();
				}
			}

			// Add any remaining items in the last chunk
			if (currentChunk.Count > 0)
			{
				result.Add(string.Join(",", currentChunk));
			}

			return result;
		}

		// Batch Request Api Controller End


		//Stock Statistics Api call Start
		public async Task<IActionResult> CreateStockStatisticsAnalystGradeJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockStatisticsAnalystGrade";
				string duration = "Monthly";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockStatisticsAnalystGrade(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockStatisticsAnalystGrade(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/stockstatistics/analystgrade/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateStockStatisticsEarnignSurprisesJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockStatisticsEarnignSurprises";
				string duration = "Annual";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockStatisticsEarnignSurprises(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockStatisticsEarnignSurprises(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/stockstatistics/earnignsurprises/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateStockStatisticsAnalystEstimateAnnualyJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockStatisticsAnalystEstimateAnnualy";
				string duration = "Annual";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockStatisticsAnalystEstimateAnnualy(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockStatisticsAnalystEstimateAnnualy(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/stockstatistics/analystestimateannualy/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateStockStatisticsAnalystEstimateQuarterlyJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockStatisticsAnalystEstimateQuarterly";
				string duration = "Quarter";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockStatisticsAnalystEstimateQuarterly(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockStatisticsAnalystEstimateQuarterly(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/stockstatistics/analystestimatequarterly/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateStockStatisticsAnalystRecommendationJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockStatisticsAnalystRecommendation";
				string duration = "Monthly";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate || nextDownloadDate == "Null" || nextDownloadDate == "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockStatisticsAnalystRecommendation(symbol));
							//break;
						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockStatisticsAnalystRecommendation(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/stockstatistics/analystrecommendation/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		//Stock Statistics Api call End

		//StockList API call Start
		public async Task<IActionResult> CreateStockListSymbolsListJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockListSymbolsList";
				string duration = "Annual";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						BackgroundJob.Enqueue(() => StockListSymbolsList());

						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			
			return RedirectToAction("Index", "CategoryWiseJob");


		}
		public async Task StockListSymbolsList()
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/stocklist/symbolslist");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateStockListAvailableListJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockListAvailableList";
				string duration = "Annual";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{

						BackgroundJob.Enqueue(() => StockListAvailableList());

						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");


		}
		public async Task StockListAvailableList()
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/stocklist/availablelist");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateStockListETFListJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockListETFList";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{

						BackgroundJob.Enqueue(() => StockListETFList()); 

						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");


		}
		public async Task StockListETFList()
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/stocklist/etflist");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		// Stock List API call end


		// Market indexes API call start
		public async Task<IActionResult> CreateMarketIndexesMajorIndexJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "MarketIndexesMajorIndex";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{

						BackgroundJob.Enqueue(() => MarketIndexesMajorIndex());

						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");


		}
		public async Task MarketIndexesMajorIndex()
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/marketindexes/majorindex");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateMarketIndexesCompanyListOfSP500Job()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "MarketIndexesCompanyListOfSP500";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{

						BackgroundJob.Enqueue(() => MarketIndexesCompanyListOfSP500());

						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");


		}
		public async Task MarketIndexesCompanyListOfSP500()
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/marketindexes/companylistofSP500");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateMarketIndexesHistoricalSP500Job()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "MarketIndexesHistoricalSP500";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{

						BackgroundJob.Enqueue(() => MarketIndexesHistoricalSP500());

						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");


		}
		public async Task MarketIndexesHistoricalSP500()
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/marketindexes/historicalSP500");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateMarketIndexesNasdaq100companiesJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "MarketIndexesNasdaq100companies";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{

						BackgroundJob.Enqueue(() => MarketIndexesNasdaq100companies());

						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");


		}
		public async Task MarketIndexesNasdaq100companies()
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/marketindexes/nasdaq100companies");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		// Market indexes API call end

		// Insider Trading API call Start
		public async Task<IActionResult> CreateInsiderTradingtransactiontypesJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "InsiderTradingtransactiontypes";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{

						BackgroundJob.Enqueue(() => InsiderTradingtransactiontypes());

						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");


		}
		public async Task InsiderTradingtransactiontypes()
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/insidertrading/transactiontypes");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateInsiderTradingSpecificSymbolJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "InsiderTradingSpecificSymbol";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => InsiderTradingSpecificSymbol(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task InsiderTradingSpecificSymbol(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/insidertrading/specificsymbol/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		// Insider Trading API call End

		//Stock Fundamentals API call start
		public async Task<IActionResult> CreateStockFundamentalsAnnuallyIncomeStatementAPIJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockFundamentalsAnnuallyIncomeStatementAPI";
				string duration = "Annual";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockFundamentalsAnnuallyIncomeStatementAPI(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockFundamentalsAnnuallyIncomeStatementAPI(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/stockfundamentals/financialstatment/annual-income-statement/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateStockFundamentalsQuarterlyIncomeStatementAPIJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockFundamentalsQuarterlyIncomeStatementAPI";
				string duration = "Quarter";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockFundamentalsQuarterlyIncomeStatementAPI(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}

			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockFundamentalsQuarterlyIncomeStatementAPI(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/stockfundamentals/financialstatment/quarter-income-statement/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateStockFundamentalsAnnuallyBalanceSheetAPIJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockFundamentalsAnnuallyBalanceSheetAPI";
				string duration = "Annual";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockFundamentalsAnnuallyBalanceSheetAPI(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockFundamentalsAnnuallyBalanceSheetAPI(string symbol)
		{
			//var symbol = "AAPL";
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/stockfundamentals/financialstatment/annual-balancesheet-statement/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateStockFundamentalsQuarterlyBalanceSheetAPIJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockFundamentalsQuarterlyBalanceSheetAPI";
				string duration = "Quarter";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockFundamentalsQuarterlyBalanceSheetAPI(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockFundamentalsQuarterlyBalanceSheetAPI(string symbol)
		{
			//var symbol = "AAPL";
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/stockfundamentals/financialstatment/quarterly-balancesheet-statement/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateStockFundamentalsAnnuallyCashFlowSheetAPIJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockFundamentalsAnnuallyCashFlowSheetAPI";
				string duration = "Annual";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockFundamentalsAnnuallyCashFlowSheetAPI(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockFundamentalsAnnuallyCashFlowSheetAPI(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/stockfundamentals/financialstatment/annually-cashFLow-statement/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateStockFundamentalsQuarterlyCashFlowSheetAPIJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "StockFundamentalsQuarterlyCashFlowSheetAPI";
				string duration = "Quarter";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockFundamentalsQuarterlyCashFlowSheetAPI(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockFundamentalsQuarterlyCashFlowSheetAPI(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/stockfundamentals/financialstatment/quarter-cashFLow-statement/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateSharesFloatJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "SharesFloat";
				string duration = "Daily";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate || nextDownloadDate == "Null" || nextDownloadDate == "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockFundamentalsSharesFloat(symbol));

						}
						//BackgroundJob.Enqueue(() => StockFundamentalsSharesFloat());
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockFundamentalsSharesFloat(string symbol)
		{
			//var symbol = "AAPL";
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/stockfundamentals/financialstatment/shares-float/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateStockFundamentalsSECFilings()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "FundHoldingsETFSectorWeightings";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => StockFundamentalsSECFillings(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task StockFundamentalsSECFillings(string symbol)
		{
			//var symbol = "AAPL";
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/stockfundamentals/financialstatment/secfillings/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		//Stock Fundamentals API call End


		// FundHoldings API call start
		public async Task<IActionResult> CreateFundHoldingsETFHoldersJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "FundHoldingsETFHolders";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => FundHoldingsETFHolders(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task FundHoldingsETFHolders(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/fundholdings/etfholders/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateFundHoldingsInstitutionalHolderJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "FundHoldingsInstitutionalHolder";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => FundHoldingsInstitutionalHolder(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task FundHoldingsInstitutionalHolder(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/fundholdings/institutionalholder/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateFundHoldingsMutualFundHolderJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "FundHoldingsMutualFundHolder";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => FundHoldingsMutualFundHolder(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task FundHoldingsMutualFundHolder(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/fundholdings/mutualfundholder/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateFundHoldingsETFSectorWeightingsJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "FundHoldingsETFSectorWeightings";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => FundHoldingsETFSectorWeightings(symbol));
							
						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			
			
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task FundHoldingsETFSectorWeightings(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/fundholdings/etfsectorweightings/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateFundHoldingsETFCountryWeightingsJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "FundHoldingsETFCountryWeightings";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => FundHoldingsETFCountryWeightings(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task FundHoldingsETFCountryWeightings(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/fundholdings/etfcountryweightings/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}

		public async Task<IActionResult> CreateFundHoldingsETFStockExposureListJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "FundHoldingsETFStockExposureList";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => FundHoldingsETFStockExposureList(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task FundHoldingsETFStockExposureList(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/fundholdings/etfstockexposureList/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}


		//Economics API call start

		public async Task<IActionResult> CreateEconomicsMarketRiskJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "EconomicsMarketRisk";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						BackgroundJob.Enqueue(() => EconomicsMarketRisk());

						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			
			return RedirectToAction("Index", "CategoryWiseJob");


		}
		public async Task EconomicsMarketRisk()
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"/api/economics/marketriskpremium");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		//Economics API call End

		//CompanyInformation api call Start

		public async Task<IActionResult> CreateCompanyInformationMarketCapitalizationJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "CompanyInformationMarketCapitalization";
				string duration = "Daily";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => CompanyInformationMarketCapitalization(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}

		public async Task CompanyInformationMarketCapitalization(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/companyinformation/marketcapitalization/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateCompanyProfileJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "CompanyProfile";
				string duration = "Daily";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate || nextDownloadDate == "Null" || nextDownloadDate == "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => CompanyProfile(symbol));
							//break;

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");
		}
		public async Task CompanyProfile(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/companyinformation/company_profile/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		//CompanyInformation API end

		//Stock Fundamentals Analysis API start
		public async Task<IActionResult> CreateAnnualCompanyFinancialRatioJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "AnnualCompanyFinancialRatio";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => AnnualCompanyFinancialRatio(symbol));
							
						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task AnnualCompanyFinancialRatio(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/financialratios/annual-company-financial-ratio/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateQuarterCompanyFinancialRatioJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "QuarterCompanyFinancialRatio";
				string duration = "Quarter";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => QuarterCompanyFinancialRatio(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task QuarterCompanyFinancialRatio(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/financialratios/quarter-company-financial-ratio/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateCompanyFinancialRatioTTMJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "CompanyFinancialRatioTTM";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => CompanyFinancialRatioTTM(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task CompanyFinancialRatioTTM(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/financialratios/company-financial-ratio-ttm/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateAnnualCompanyEnterpriseValueJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "AnnualCompanyEnterpriseValue";
				string duration = "Annual";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => AnnualCompanyEnterpriseValue(symbol));
							
						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task AnnualCompanyEnterpriseValue(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/company-enterprise-value/annual/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreatequarterCompanyEnterpriseValueJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "quarterCompanyEnterpriseValue";
				string duration = "Quarter";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => quarterCompanyEnterpriseValue(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task quarterCompanyEnterpriseValue(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/company-enterprise-value/quarter/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateIncomeStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "IncomeStatementGrowth";
				string duration = "Annual";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => IncomeStatementGrowth(symbol));
							
						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task IncomeStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/financial-statement-growth/income-statement-growth/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateCashFlowStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "CashFlowStatementGrowth";
				string duration = "Annual";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => CashFlowStatementGrowth(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task CashFlowStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/financial-statement-growth/cash-flow-growth/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateCompanyKeyMetricsTTMStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "CompanyKeyMetricsTTMStatementGrowth";
				string duration = "Monthly";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => CompanyKeyMetricsTTMStatementGrowth(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task CompanyKeyMetricsTTMStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/key-metrics/Company-ttm-key-metrics/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateAnnualCompanyKeyMetricsStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "AnnualCompanyKeyMetricsStatementGrowth";
				string duration = "Annual";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => AnnualCompanyKeyMetricsStatementGrowth(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task AnnualCompanyKeyMetricsStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/key-metrics/annual-company/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateQuarterCompanyKeyMetricsStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "QuarterCompanyKeyMetricsStatementGrowth";
				string duration = "Quarter";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => QuarterCompanyKeyMetricsStatementGrowth(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task QuarterCompanyKeyMetricsStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/key-metrics/quarter-company/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateAnnualCompanyFinancialGrowthStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "AnnualCompanyFinancialGrowthStatementGrowth";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => AnnualCompanyFinancialGrowthStatementGrowth(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task AnnualCompanyFinancialGrowthStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/financial-growth/annual-company/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateQuarterCompanyFinancialGrowthStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "QuarterCompanyFinancialGrowthStatementGrowth";
				string duration = "Quarter";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => QuarterCompanyFinancialGrowthStatementGrowth(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task QuarterCompanyFinancialGrowthStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/financial-growth/quarter-company/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateHistoricCompanyRatingStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "HistoricCompanyRatingStatementGrowth";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => HistoricCompanyRatingStatementGrowth(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task HistoricCompanyRatingStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/rating/historic-companies-rating/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateDailyCompanyRatingStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "DailyCompanyRatingStatementGrowth";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => DailyCompanyRatingStatementGrowth(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task DailyCompanyRatingStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/rating/daily-companies-rating/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateAnnualCompaniesHistoricalDCFStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "AnnualCompaniesHistoricalDCFStatementGrowth";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => AnnualCompaniesHistoricalDCFStatementGrowth(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task AnnualCompaniesHistoricalDCFStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/dcf/annual-companies-historical-dcf/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateQuarterlyCompaniesHistoricalDCFStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "QuarterlyCompaniesHistoricalDCFStatementGrowth";
				string duration = "Quarter";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => QuarterlyCompaniesHistoricalDCFStatementGrowth(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task QuarterlyCompaniesHistoricalDCFStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/dcf/quarterly-companies-historical-dcf/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateDaillyCompaniesHistoricalDCFStatementGrowthJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "DaillyCompaniesHistoricalDCFStatementGrowth";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => DaillyCompaniesHistoricalDCFStatementGrowth(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task DaillyCompaniesHistoricalDCFStatementGrowth(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/dcf/daily-companies-historical-dcf/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		//Stock Fundamentals Analysis ends here
		// DCF starts here
		public async Task<IActionResult> CreateCompaniesDCFJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "CompaniesDCF";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => CompaniesDCF(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task CompaniesDCF(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/dcf/companies-dcf/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateAdvanceDCFprojectionIncludingWACCJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "AdvanceDCFprojectionIncludingWACC";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => AdvanceDCFprojectionIncludingWACC(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task AdvanceDCFprojectionIncludingWACC(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/dcf/advance-dcf-projection-including-wacc/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		public async Task<IActionResult> CreateAdvanceLeveredDCFprojectionIncludingWACCJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "AdvanceLeveredDCFprojectionIncludingWACC";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate||nextDownloadDate=="Null"||nextDownloadDate== "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => AdvanceLeveredDCFprojectionIncludingWACC(symbol));

						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task AdvanceLeveredDCFprojectionIncludingWACC(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/StockFundamentalsAnalysis/dcf/advance-levered-dcf-projection-including-wacc/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		//DCF part ends here
		
		//Earnings calendar part starts here
		public async Task<IActionResult> CreateEarningsCalendarJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "EarningsCalender";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate || nextDownloadDate == "Null" || nextDownloadDate == "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						//foreach (var symbol in symbols)
						//{
						//	BackgroundJob.Enqueue(() => EarningsCalender(symbol));

						//}
						BackgroundJob.Enqueue(() => EarningsCalender());
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task EarningsCalender()
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/earnings/calendar/");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		//Earnings calendar part ends here

		//Technical Indicator part starts here
		public async Task<IActionResult> CreateRSIDataJob()
		{
			try
			{
				using var client = _apiConnection.CreateHttpClient();
				string apiName = "RSIData";
				string duration = "Daily";
				var inActiveStatusResponse = await client.GetAsync($"api/tblApi/getInActiveStatus/{apiName}");
				if (inActiveStatusResponse.IsSuccessStatusCode)
				{
					string inActiveStatus = await inActiveStatusResponse.Content.ReadAsStringAsync();
					if (inActiveStatus == "True")
					{
						return RedirectToAction("Index", "CategoryWiseJob");
					}
				}
				var response = await client.GetAsync($"api/tblApi/getNextDownloadDate/{apiName}");
				if (response.IsSuccessStatusCode)
				{
					string nextDownloadDate = await response.Content.ReadAsStringAsync();
					string today = DateTime.Now.ToString("yyyy-MM-dd");
					if (today == nextDownloadDate || nextDownloadDate == "Null" || nextDownloadDate == "DatePassed")
					{
						var symbols = await GetOrSetSymbolsInCache();
						foreach (var symbol in symbols)
						{
							BackgroundJob.Enqueue(() => RSIDataDownload(symbol));
							//break;
						}
						var updateTblApiResponse = await client.GetAsync($"api/tblApi/updateNextDownloadDate/{apiName}/{duration}");
						if (updateTblApiResponse.IsSuccessStatusCode)
						{
							Console.WriteLine("Operation Successful");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured during the operation");
			}
			return RedirectToAction("Index", "CategoryWiseJob");

		}
		public async Task RSIDataDownload(string symbol)
		{
			using var client = _apiConnection.CreateHttpClient();
			var response = await client.GetAsync($"api/technicalIndicator/RSI/{symbol}");
			if (!response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var error_message = JsonConvert.DeserializeObject<ErrorModel>(responseData);
				Console.WriteLine("Error");
				throw new Exception(error_message.Message);
			}
			else
			{
				Console.WriteLine("Sueccess");
			}
		}
		//Technical Indicator part ends here
	}
}

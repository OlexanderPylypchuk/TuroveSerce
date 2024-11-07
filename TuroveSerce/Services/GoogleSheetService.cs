using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;

namespace TuroveSerce.Bot.Services
{
	public class GoogleSheetService
	{
		private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
		private const string ApplicationName = SD.ApplicationName;
		private readonly SheetsService _sheetsService;

		public GoogleSheetService()
		{
			_sheetsService = InitializeSheetsService();
		}

		private SheetsService InitializeSheetsService()
		{
			string serviceAccountJson = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_KEY");
			var credential = GoogleCredential.FromJson(serviceAccountJson)
				.CreateScoped(SheetsService.Scope.Spreadsheets);

			return new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = ApplicationName,
			});
		}

		public async Task AppendOrder(string spreadsheetId, string item, string deliveryMethod, string city, string phoneNumber, string firstName, string lastName, string count, string chatId)
		{
			var orderDetails = new List<object>
			{
				DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
				item,
				deliveryMethod,
				city,
				phoneNumber,
				firstName,
				lastName,
				count,
				chatId
			};
			var valueRange = new ValueRange { Values = new List<IList<object>> { orderDetails } };

			var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, spreadsheetId, $"!A:I");
			appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

			await appendRequest.ExecuteAsync();
			Console.WriteLine("Order appended successfully to Google Sheet.");
		}
	}
}

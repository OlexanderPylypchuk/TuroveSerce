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
			var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
				new ClientSecrets()
				{
					ClientId = SD.ClientId,
					ClientSecret = SD.ClientSecrets
				},
				Scopes,
				"user",
				CancellationToken.None,
				new FileDataStore("token.json", true)).Result;

			return new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = ApplicationName,
			});
		}

		public void AppendOrder(string spreadsheetId, string sheetName, string firstName, string lastName, string address, string phoneNumber, string item, string chatId)
		{
			var orderDetails = new List<object>
			{
				DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
				firstName,
				lastName,
				address,
				phoneNumber,
				item,
				chatId
			};
			var valueRange = new ValueRange { Values = new List<IList<object>> { orderDetails } };

			var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, spreadsheetId, $"!A:H");
			appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

			appendRequest.Execute();
			Console.WriteLine("Order appended successfully to Google Sheet.");
		}
	}
}

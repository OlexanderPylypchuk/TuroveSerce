using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TuroveSerce.Bot.Services;

namespace TuroveSerce.Bot
{
	class Program
	{
		private static ITelegramBotClient _botClient;
		static ReplyKeyboardMarkup replyKeyboardMarkup;
		static ReplyKeyboardMarkup orderKeyboardMarkup;
		private static ReceiverOptions _receiverOptions;
		private static GoogleSheetService _googleSheetService;

		static async Task Main()
		{
			_googleSheetService = new GoogleSheetService();
			_botClient = new TelegramBotClient(SD.Token);
			_receiverOptions = new ReceiverOptions
			{
				AllowedUpdates = new[]
				{
				UpdateType.Message
			},
				ThrowPendingUpdates = true,
			};

			using var cts = new CancellationTokenSource();
			replyKeyboardMarkup = new(new[]
			{
				new KeyboardButton("Оформити замовлення")
			})
			{
				ResizeKeyboard = true
			};
			orderKeyboardMarkup = new(new[]
			{
				new KeyboardButton("Приклад оформлення")
			})
			{
				ResizeKeyboard = true
			};
			_botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);
			var me = await _botClient.GetMeAsync();
			Console.WriteLine($"{me.FirstName}");
			await Task.Delay(-1);
		}

		private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			try
			{
				ChatId chatId = update.Message.Chat.Id;
				if (chatId == SD.GroupChatId)
				{
					await AdminResponce(update);
				}
				else
				{
					await UserRequest(update, chatId);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		private async static Task AdminResponce(Update update)
		{
			if (update.Message.ReplyToMessage != null
				&& update.Message.ReplyToMessage.From.Username == SD.BotName)
			{
				string chatId;
				if (update.Message.ReplyToMessage == null)
				{
					await _botClient.SendTextMessageAsync(SD.GroupChatId, "Оберіть повідомлення, до якого звертаєтесь");
					return;
				}
				if (update.Message.ReplyToMessage.Text == null)
				{
					chatId = update.Message.ReplyToMessage.Caption.Split('#').Last();
				}
				else
				{
					chatId = update.Message.ReplyToMessage.Text.Split('#').Last();
				}
				if (update.Message.Text == "Підтверджено")
				{
					string caption = update.Message.ReplyToMessage.Caption;
					string item = ExtractDetail(caption, @"(?<=Назва товару:\s)(.*?)(?=\n|$)");
					string count = ExtractDetail(caption, @"(?<=Кількість товару:\s)(.*?)(?=\n|$)");
					string deliveryMethod = ExtractDetail(caption, @"(?<=Місце отримання:\s)(.*?)(?=\n|$)");
					string city = ExtractDetail(caption, @"(?<=Населений пункт:\s)(.*?)(?=\n|$)");
					string phoneNumber = ExtractDetail(caption, @"(?<=Номер телефону:\s)(0\d{9})(?=\n|$)");
					string firstName = ExtractDetail(caption, @"(?<=Ім'я:\s)(.*?)(?=\n|$)");
					string lastName = ExtractDetail(caption, @"(?<=Прізвище:\s)(.*?)(?=\n|$)");

					await _googleSheetService.AppendOrder(SD.SheetId, item, deliveryMethod, city, phoneNumber, firstName, lastName, count, chatId);
					await _botClient.SendTextMessageAsync(chatId, "Замовлення підтверджено!", replyMarkup: replyKeyboardMarkup);
					await _botClient.SendTextMessageAsync(SD.GroupChatId, "Опрацьовано");
					return;
				}
				await _botClient.SendTextMessageAsync(chatId, update.Message.Text, replyMarkup: replyKeyboardMarkup);
				await _botClient.SendTextMessageAsync(SD.GroupChatId, "Повідомлення надіслано");
			}
		}

		private async static Task UserRequest(Update update, ChatId chatId)
		{
			if (update.Message.Text != null)
			{
				if (update.Message.Text == "Оформити замовлення")
				{
					await _botClient.SendTextMessageAsync(chatId, "Що б оформити замовлення необхідно:\n" +
						"1.Надіслати зображення про оплату за посиланням: https://send.monobank.ua/jar/33dEGgTsLr або за реквізитами 4441 1111 2796 9008;\n" +
						"2.В описі додати без лапок КЛЮЧ - СЛОВО «Замовлення»;\n" +
						"3.Вказати у тому ж повідомленні свою контактну інформацію у форматі:", replyToMessageId: update.Message.MessageId);
					await _botClient.SendTextMessageAsync(chatId, "Назва товару: (назва позиції товару)\n" +
						"Місце отримання: (відділення, поштомат, адреса з вказанням номера)\n" +
						"Населений пункт: (назва міста/села/смт, при необхідності вказанням області та району)\n" +
						"Номер телефону: у такому форматі (0980000000)\n" +
						"Ім'я: (Ваше імя)\n" +
						"Прізвище: (Ваше Прізвище)\n"+
						"Кількість товару: (Кількість товару в шт)", replyMarkup:new ReplyKeyboardRemove());
					return;
				}
				if (update.Message.Text == "/start")
				{
					await _botClient.SendTextMessageAsync(chatId, "Слава Ісусу Христу!\nЦей бот допоможе оформити замовлення та дізнатись необхідну інформацію. Для оформлення замовлення натисність кнопку «Оформити замовлення».\nТакож цей бот працює як зворотній звʼязок з адміністрацією.", replyToMessageId: update.Message.MessageId, replyMarkup: replyKeyboardMarkup);
					return;
				}
				if(update.Message.Text == "Приклад оформлення")
				{
					await _botClient.SendPhotoAsync(chatId, InputFile.FromUri(SD.ExampleImageUri),caption:"Замовлення\r\n" +
						"Назва товару: Aequo animo\r\n" +
						"Місце отримання: Поштомат 37795\r\n" +
						"Населений пункт: Бровари, Київська область\r\n" +
						"Номер телефону: 0980000000\r\n" +
						"Ім'я: Ісус\r\n" +
						"Прізвище: Христос\r\n" +
						"Кількість товару: 1", replyMarkup: new ReplyKeyboardRemove());
					return;
				}
				string message = $"{update.Message.Text}\n" +
					$"@{update.Message.Chat.Username}, {update.Message.Chat.FirstName} {update.Message.Chat.LastName} #{update.Message.Chat.Id}";
				await _botClient.SendTextMessageAsync(SD.GroupChatId, message);
				await _botClient.SendTextMessageAsync(chatId, "Надіслано, очікуйте відповіді", replyMarkup: replyKeyboardMarkup);
			}
			else if (update.Message.Photo != null)
			{
				if (update.Message.Caption != null && update.Message.Caption.Contains("Замовлення"))
				{
					await OrderRequest(update, chatId);
					return;
				}

				var image = update.Message.Photo.Last();
				string message = $"{update.Message.Caption}\n" +
					$"@{update.Message.Chat.Username}, {update.Message.Chat.FirstName} {update.Message.Chat.LastName} #{update.Message.Chat.Id}";
				await _botClient.SendPhotoAsync(SD.GroupChatId, InputFile.FromFileId(image.FileId), caption: message);
				await _botClient.SendTextMessageAsync(chatId, "Надіслано, очікуйте відповіді", replyMarkup: replyKeyboardMarkup);
			}
			else
			{
				await _botClient.SendTextMessageAsync(chatId, "Бот не підтримує даний формат комунікації", replyMarkup: replyKeyboardMarkup);
			}
		}

		private async static Task OrderRequest(Update update, ChatId chatId)
		{
			if (update.Message.Caption == null || update.Message.Photo == null)
			{
				await _botClient.SendTextMessageAsync(chatId, "Будь ласка, надішліть зображення оплати з описом, що містить ім'я, прізвище та адресу доставки.", replyMarkup: replyKeyboardMarkup);
				return;
			}

			string caption = update.Message.Caption;
			string deliveryMethod = ExtractDetail(caption, @"(?<=Місце отримання:\s)(.*?)(?=\n|$)");
			string count = ExtractDetail(caption, @"(?<=Кількість товару:\s)(.*?)(?=\n|$)");
			string firstName = ExtractDetail(caption, @"(?<=Ім'я:\s)(.*?)(?=\n|$)");
			string lastName = ExtractDetail(caption, @"(?<=Прізвище:\s)(.*?)(?=\n|$)");
			string city = ExtractDetail(caption, @"(?<=Населений пункт:\s)(.*?)(?=\n|$)");
			string phoneNumber = ExtractDetail(caption, @"(?<=Номер телефону:\s)(0\d{9})(?=\n|$)");
			string item = ExtractDetail(caption, @"(?<=Назва товару:\s)(.*?)(?=\n|$)");

			if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName)
				|| string.IsNullOrEmpty(city) || string.IsNullOrEmpty(phoneNumber)
				|| string.IsNullOrEmpty(item) || string.IsNullOrEmpty(deliveryMethod)
				|| string.IsNullOrEmpty(count))
			{
				await _botClient.SendTextMessageAsync(chatId, "Будь ласка, переконайтеся, дані введені правильно, та спробуйте знову. Якщо у вас виникають проблеми, натисніть кнопку, що б отримати зразок", replyMarkup: orderKeyboardMarkup);
				return;
			}

			var paymentImage = update.Message.Photo.Last();

			string message = $"Замовлення отримано:\n" +
							 $"Назва товару: {item}\n" +
							 $"Місце отримання: {deliveryMethod}\n" +
							 $"Населений пункт: {city}\n" +
							 $"Номер телефону: {phoneNumber}\n" +
							 $"Ім'я: {firstName}\n" +
							 $"Прізвище: {lastName}\n" +
							 $"Кількість товару: {count}\n" +
							 $"@{update.Message.Chat.Username}, {update.Message.Chat.FirstName} {update.Message.Chat.LastName} #{update.Message.Chat.Id}";

			await _botClient.SendPhotoAsync(SD.GroupChatId, InputFile.FromFileId(paymentImage.FileId), caption: message);
			await _botClient.SendTextMessageAsync(chatId, "Ваше замовлення отримано та передано адміністратору.", replyMarkup: replyKeyboardMarkup);
		}

		private static string ExtractDetail(string input, string pattern)
		{
			var match = Regex.Match(input, pattern);
			return match.Success ? match.Value.Trim() : string.Empty;
		}

		private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
		{
			var ErrorMessage = error switch
			{
				ApiRequestException apiRequestException
					=> $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
				_ => error.ToString()
			};

			Console.WriteLine(ErrorMessage);
			return Task.CompletedTask;
		}
	}
}
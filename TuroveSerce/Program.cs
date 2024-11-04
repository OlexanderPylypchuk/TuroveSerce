using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace TuroveSerce.Bot
{
	class Program
	{
		private static ITelegramBotClient _botClient;
		static ReplyKeyboardMarkup replyKeyboardMarkup;
		private static ReceiverOptions _receiverOptions;

		static async Task Main()
		{
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
			string chatId;
			if(update.Message.ReplyToMessage == null)
			{
				await _botClient.SendTextMessageAsync(SD.GroupChatId, "Оберіть повідомлення, до якого звертаєтесь");
				return;
			}
			if(update.Message.ReplyToMessage.Text == null)
			{
				chatId = update.Message.ReplyToMessage.Caption.Split('#').Last();
			}
			else
			{
				chatId = update.Message.ReplyToMessage.Text.Split('#').Last();
			}
			await _botClient.SendTextMessageAsync(chatId, update.Message.Text, replyMarkup: replyKeyboardMarkup);
			await _botClient.SendTextMessageAsync(SD.GroupChatId, "Повідомлення надіслано");
		}

		private async static Task UserRequest(Update update, ChatId chatId)
		{
			if (update.Message.Text != null)
			{
				if(update.Message.Text == "Оформити замовлення")
				{
					await _botClient.SendTextMessageAsync(chatId, "Що б оформити замовлення необхідно: \n1. Надіслати зображення про оплату\n" +
						"2. В описі додати слово 'Замовлення' \n3. Вказати у тому ж повідомленні свою контактну інформацію у форматі:\n " +
						"Ім'я: [Ваше ім'я]\n" +
						"Прізвище: [Ваше прізвище]\n" +
						"Адреса: [Ваша адреса, або адреса нової пошти]\n" +
						"Номер телефону: [Ваш номер телефону]\n" +
						"Товар: [Назва товару]", replyToMessageId:update.Message.MessageId);
					return;
				}
				if(update.Message.Text == "/start")
				{
					await _botClient.SendTextMessageAsync(chatId, "Слава Ісусу Христу! Цей бот допоможе оформити замовлення та дізнатись необхідну інформацію! Що б дізнатись більше про товари, що Вас цікавлять, надішліть повідомлення з запитанням.", replyToMessageId: update.Message.MessageId, replyMarkup: replyKeyboardMarkup);
					return;
				}

				string message = $"{update.Message.Text}\n" +
					$"@{update.Message.Chat.Username}, {update.Message.Chat.FirstName} {update.Message.Chat.LastName} #{update.Message.Chat.Id}";
				await _botClient.SendTextMessageAsync(SD.GroupChatId, message);
				await _botClient.SendTextMessageAsync(chatId, "Надіслано, очікуйте відповіді", replyMarkup: replyKeyboardMarkup);
			}
			else if(update.Message.Photo != null)
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
			string firstName = ExtractDetail(caption, @"(?<=Ім'я:\s)(.*?)(?=\n|$)");
			string lastName = ExtractDetail(caption, @"(?<=Прізвище:\s)(.*?)(?=\n|$)");
			string address = ExtractDetail(caption, @"(?<=Адреса:\s)(.*?)(?=\n|$)");
			string phoneNumber = ExtractDetail(caption, @"(?<=Номер телефону:\s)(\+?380\d{9}|0\d{9})(?=\n|$)");
			string item = ExtractDetail(caption, @"(?<=Товар:\s)(.*?)(?=\n|$)");

			if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(address) || string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(item))
			{
				await _botClient.SendTextMessageAsync(chatId, "Будь ласка, переконайтеся, що опис містить ім'я, прізвище, адресу доставки, номер телефону та назву товару.", replyMarkup: replyKeyboardMarkup);
				return;
			}

			var paymentImage = update.Message.Photo.Last();

			string message = $"Замовлення отримано:\n" +
							 $"Ім'я: {firstName}\n" +
							 $"Прізвище: {lastName}\n" +
							 $"Адреса доставки: {address}\n" +
							 $"Номер телефону: {phoneNumber}\n" +
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
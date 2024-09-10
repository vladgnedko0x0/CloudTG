using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.ReplyMarkups;
using System.Threading;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Dapper;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;
using System.Configuration;
using System.Data.SqlClient;
using Lib;
using Newtonsoft.Json;

namespace CloudTG
{
    internal class Program
    {
        
        static ITelegramBotClient bot = new TelegramBotClient(Token);
        static string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TelegramCloud;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False";
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc1251 = Encoding.GetEncoding(1251);

            System.Console.OutputEncoding = System.Text.Encoding.UTF8;
            System.Console.InputEncoding = enc1251;
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };

            bot.StartReceiving(
       HandleUpdateAsync,
       HandleErrorAsync,
       receiverOptions,
       cancellationToken
   );
            Console.ReadKey();
        }

        public static async Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
          
            try
            {
                if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                {
                    var message = update.Message;
                    Console.WriteLine("Name = {0} |\tTag = {1} |\tText = {2} |\tID = {3}", message.From.FirstName, message.From.Username, message.Text, message.From.Id);
                      if(!Commands.UpdateInformationOfUser(client, message, new SqlConnection(connectionString)).Result)
                    {
                        return;
                    }
                    
                        switch (message.Type)
                        {
                            case MessageType.Unknown:
                                await client.SendTextMessageAsync(message.Chat, "Извини, но что это?");
                                break;
                            case MessageType.Text:
                                await Task.Run(() => { ChekerMessageUpdate.TextMessage(client, message, connectionString); });
                                break;
                            case MessageType.Photo:
                                await Task.Run(() => { ChekerMessageUpdate.PhotoMessage(client, message, connectionString); });

                                break;
                            case MessageType.Audio:
                                await Task.Run(() => { ChekerMessageUpdate.AudioMessage(client, message, connectionString); });
                                break;
                            case MessageType.Video:
                                await Task.Run(() => { ChekerMessageUpdate.VideoMessage(client, message, connectionString); });
                                break;
                            case MessageType.Voice:
                                await Task.Run(() => { ChekerMessageUpdate.VoiceMessage(client, message, connectionString); });
                                break;
                            case MessageType.Document:
                                await Task.Run(() => { ChekerMessageUpdate.DocumentMessage(client, message, connectionString); });
                                break;
                            case MessageType.Sticker:
                                await Task.Run(() => { ChekerMessageUpdate.StickerMessage(client, message, connectionString); });
                                break;
                            case MessageType.Location:
                                break;
                            case MessageType.Contact:
                                break;
                            case MessageType.Venue:
                                break;
                            case MessageType.Game:
                                break;
                            case MessageType.VideoNote:
                            await Task.Run(() => { ChekerMessageUpdate.VideoNoteMessage(client, message, connectionString); });
                            break;
                            case MessageType.Invoice:
                                break;
                            case MessageType.SuccessfulPayment:
                                break;
                            case MessageType.WebsiteConnected:
                                break;
                            case MessageType.ChatMembersAdded:
                                break;
                            case MessageType.ChatMemberLeft:
                                break;
                            case MessageType.ChatTitleChanged:
                                break;
                            case MessageType.ChatPhotoChanged:
                                break;
                            case MessageType.MessagePinned:
                                break;
                            case MessageType.ChatPhotoDeleted:
                                break;
                            case MessageType.GroupCreated:
                                break;
                            case MessageType.SupergroupCreated:
                                break;
                            case MessageType.ChannelCreated:
                                break;
                            case MessageType.MigratedToSupergroup:
                                break;
                            case MessageType.MigratedFromGroup:
                                break;
                            case MessageType.Poll:
                                break;
                            case MessageType.Dice:
                                break;
                            case MessageType.MessageAutoDeleteTimerChanged:
                                break;
                            case MessageType.ProximityAlertTriggered:
                                break;
                            case MessageType.WebAppData:
                                break;
                            case MessageType.VideoChatScheduled:
                                break;
                            case MessageType.VideoChatStarted:
                                break;
                            case MessageType.VideoChatEnded:
                                break;
                            case MessageType.VideoChatParticipantsInvited:
                                break;
                            default:
                                break;
                        }
                    }
                    

                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToUpper());
            }
            
        }
    }
}

using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CloudTG
{
    internal static class ChekerMessageUpdate
    {
        public  static async void TextMessage(ITelegramBotClient client, Message message, string connectionDB)
        {
            try
            {
                SqlConnection sql = new SqlConnection(connectionDB);
                //if (HaveInAccess(client,message,sql).Result)
                //{
                if (message.Text=="/start")
                {
                    Commands.Start(client, message, sql);
                    return;
                }
                if (ExistInAccess(client, message, sql).Result)
                {
                    switch (message.Text)
                    {
                        case "/start": break;

                        case "Магазин": Commands.Shop(client, message, sql); break;
                        case "Главная": Commands.Main(client, message, sql); break;
                        case "Акаунт": Commands.Account(client, message, sql); break;
                        case "Коины": Commands.CoinsShop(client, message, sql); break;
                        case "Подписка": Commands.Sub(client, message, sql); break;
                        case "Назад": Commands.Back(client, message, sql); break;
                        case "Купить": Commands.Buy(client, message, sql); break;
                        case "Хранилище": Commands.Storage(client, message, sql); break;
                        default: Commands.CheckStep(client, message, sql); break;
                    }
                }
                
                //}
                //else
                //{
                //    Message message1 = new Message();
                //    message1 = message;
                //    message1.Text = message.From.Username;
                //    await client.SendTextMessageAsync(message.From.Id, "У вас нету доступа!!!");
                //    Commands.ConnectToAccount(client,message1,sql);
                //}
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToUpper());
            }
        }
        public async static  void VideoMessage(ITelegramBotClient client,Message message,string connectionDB)
        {
            SqlConnection sql = new SqlConnection(connectionDB);
            if (Access(client, message, sql).Result)
            {
                Commands.AddInStorageVideo(client, message, sql);
            }
        }


        internal async static void AudioMessage(ITelegramBotClient client, Message message, string connectionDB)
        {
            SqlConnection sql = new SqlConnection(connectionDB);
            if (Access(client, message, sql).Result)
            {
                Commands.AddInStorageAudio(client, message, sql);
            }
        }

        internal async static void VoiceMessage(ITelegramBotClient client, Message message, string connectionDB)
        {
            SqlConnection sql = new SqlConnection(connectionDB);
            if (Access(client, message, sql).Result)
            {
                Commands.AddInStorageVoice(client, message, sql);
            }
        }

        internal async  static void PhotoMessage(ITelegramBotClient client, Message message, string connectionDB)
        {
            SqlConnection sql = new SqlConnection(connectionDB);
            if (Access(client, message, sql).Result)
            {
                Commands.AddInStoragePhoto(client, message, sql);
            }
        }

        internal async static void DocumentMessage(ITelegramBotClient client, Message message, string connectionDB)
        {
            SqlConnection sql = new SqlConnection(connectionDB);
            if (Access(client, message, sql).Result)
            {
                Commands.AddInStorageDocument(client, message, sql);
            }
        }
        public async static Task<bool> Access(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
            Lib.Access access = null;
            if (CurrentUser.TelegramId != CurrentUser.CurrentAccountId)
            {
                access = sql.QueryFirstOrDefault<Lib.Access>("select * from Access where MainUserId=@mui and RefUserId=@rui", new { mui = CurrentUser.CurrentAccountId, rui = CurrentUser.TelegramId });
            }
            else
            {
                return true;
            }

            if (access == null)
            {
                access = sql.QueryFirstOrDefault<Lib.Access>("select * from Access where MainUserId=@mui and RefUserId=@mui", new { mui = CurrentUser.CurrentAccountId });
                if (access == null)
                {
                    await client.SendTextMessageAsync(message.Chat, "У вас нету доступа!!!");
                    Message message1 = new Message();
                    message1 = message;
                    message1.Text = message.From.Username;
                    
                    Commands.ConnectToAccount(client, message1, sql);
                }
                else
                {
                    if (access.General == true)
                    {
                        await client.SendTextMessageAsync(message.Chat, "У вас нету прав на это действие!!!");
                    }
                    else
                    {
                        if (access.RuleUser == false)
                        {
                            await client.SendTextMessageAsync(message.Chat, "У вас нету прав на это действие!!!");
                        }
                        else
                        {
                            return true;
                        }
                    }
                }

            }
            else
            {
                if (access.General == true)
                {
                    await client.SendTextMessageAsync(message.Chat, "У вас нету прав на это действие!!!");
                }
                else
                {
                    if (access.RuleUser == false)
                    {
                        await client.SendTextMessageAsync(message.Chat, "У вас нету прав на это действие!!!");
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;

        }

        internal static void VideoNoteMessage(ITelegramBotClient client, Message message, string connectionString)
        {
            SqlConnection sql = new SqlConnection(connectionString);
            if (Access(client, message, sql).Result)
            {
                Commands.AddInStorageVideoNote(client, message, sql);
            }
        }

        public async  static Task< List<Lib.Access>> AccessGeter(ITelegramBotClient client, Message message, SqlConnection sql)
        {
           Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
           List<Lib.Access> access =null;
                access= sql.Query<Lib.Access>("select * from Access where MainUserId=@mui", new { mui = CurrentUser.TelegramId }).ToList();
            return access;
        }
         public async static Task<bool> HaveInAccess(ITelegramBotClient client ,Message message,SqlConnection sql)
        {
            Lib.User CurrentUser = await sql.QueryFirstOrDefaultAsync<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            if (CurrentUser==null)
            {

            }
            if (CurrentUser.TelegramId==CurrentUser.CurrentAccountId)
            {
                return true;
            }
            Lib.User user = await sql.QueryFirstOrDefaultAsync<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
            Lib.Access access= await sql.QueryFirstOrDefaultAsync<Lib.Access>("select * from Access where MainUserId=@mui and RefUserId=@rui or MainUserId=@mui and General='true'", new { mui=user.TelegramId,rui=CurrentUser.TelegramId });
            if (access!=null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        internal async  static void StickerMessage(ITelegramBotClient client, Message message, string connectionDB)
        {
            SqlConnection sql = new SqlConnection(connectionDB);
            if (Access(client,message,sql).Result)
            {
                Commands.AddInStorageSticker(client, message, sql);
            }   
        }
        public static async Task<bool> ExistInAccess(ITelegramBotClient client,Message message,SqlConnection sql)
        {
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
            Lib.Access access = null;
            if (CurrentUser.TelegramId != CurrentUser.CurrentAccountId)
            {
                access = sql.QueryFirstOrDefault<Lib.Access>("select * from Access where MainUserId=@mui and RefUserId=@rui", new { mui = CurrentUser.CurrentAccountId, rui = CurrentUser.TelegramId });
            }
            else
            {
                return true;
            }

            if (access == null)
            {
                access = sql.QueryFirstOrDefault<Lib.Access>("select * from Access where MainUserId=@mui and RefUserId=@mui", new { mui = CurrentUser.CurrentAccountId });
                if (access == null)
                {
                    await client.SendTextMessageAsync(message.Chat, "У вас нету доступа!!!");
                    Message message1 = new Message();
                    message1 = message;
                    message1.Text = message.From.Username;

                    Commands.ConnectToAccount(client, message1, sql);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
                return false;
        }

    }
}

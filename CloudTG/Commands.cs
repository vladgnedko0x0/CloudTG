using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using Lib;
using System.Threading;

namespace CloudTG
{
    internal static class Commands
    {
        public async static Task<bool> UpdateInformationOfUser(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            if (message.From.Username == null)
            {
               
                //if (message.From.Id == creator[0].Id) { }
                await client.DeleteMessageAsync(message.Chat, message.MessageId);
                //await client.SendTextMessageAsync(message.Chat, message.From.FirstName + "\nПожалуйста настройте свой username(тег), и напишите снова\nБез него не будут приходить уведомления о гуляках");
                return false;
            }

            if (sql.QueryFirstOrDefault<Lib.User>("select * " +
             "from Users " +
             "where TelegramId=@p", new { p = message.From.Id }) != null)
            {
                Lib.User user = sql.QueryFirstOrDefault<Lib.User>("select * " +
                "from Users " +
                "where TelegramId=@p", new { p = message.From.Id });
                if (user.FirstName != message.From.FirstName)
                {
                    sql.Execute("update Users " +
              "Set FirstName=@fn " +
              "where  TelegramId=@ti", new { fn = message.From.FirstName, ti = message.From.Id });
                }
                if (user.Username != message.From.Username)
                {
                    sql.Execute("update Users " +
              "Set Username=@us " +
              "where  TelegramId=@ti ", new { us = (string)message.From.Username, ti = message.From.Id });
                }

            }
            return true;
        }
        public static async void Start(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            try
            {
                
                ReplyKeyboardMarkup replyMarkup = new[] {
                            new[] { "Магазин","Акаунт" },
                            new[] { "Хранилище" }
                           };
                if (sql.QueryFirstOrDefault<Lib.User>("select Id " +
                 "from Users " +
                 "where TelegramId=@p", new { p = message.From.Id }) == null)
                {
                    if (message.From.Username != null)
                    {

                        sql.Execute("insert into Users (FirstName,Username,TelegramId,CountItems,Coins,Step,CurrentDir,PayStatus,CurrentAccountId) " +
                        "values(@fn,@un,@ti,@ci,@cs,@st,@cr,@ps,@cai)",
                        new
                        {
                            fn = message.From.FirstName,
                            un = message.From.Username,
                            ti = message.From.Id,
                            ci = 25,
                            cs = 0,
                            st = 0,
                            cr = "Storage/",
                            ps = -1,
                            cai=message.From.Id
                        });

                        await client.SendTextMessageAsync(message.Chat, "Добро пожаловать " + message.From.FirstName + " в \"Telegram Cloud\"\n" +
                            "Загружай файли, создавай папки пользуйся!", replyMarkup: replyMarkup);
                    }
                    else
                    {
                        await client.SendTextMessageAsync(message.Chat, "Пожалуйста настройте свой username(тег), и нажмите старт снова");
                    }

                }
                else
                {
                    await client.SendTextMessageAsync(message.Chat, "С возвращениям " + message.From.FirstName + " в \"Telegram Cloud\"", replyMarkup: replyMarkup);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
           
        }

        public async static void AddInStorageAudio(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
            if (CurrentUser.Step == 5 && user.CountItems != 0)
            {
                string FileName = DateTime.Now.ToString() + ".audio";
                if (message.Audio.FileName != null)
                {
                    FileName = message.Audio.FileName;
                }
                sql.Execute("insert into Files " +
                    "(FileId,FileName,FilePath,OwerId,Type) " +
                    "values(@fi,@fn,@fp,@oi,@tp)",
                    new
                    {
                        fi = message.Audio.FileId,
                        fn = FileName,
                        oi = user.Id,
                        fp = CurrentUser.CurrentDir,
                        tp = "Audio"
                    });
                if (user.PayStatus < 4)
                {
                    sql.Execute("update Users set " +
                   "CountItems=@ci where TelegramId=@ti",
                   new
                   {
                       ti = CurrentUser.CurrentAccountId,
                       ci = user.CountItems - 1
                   });
                }
                await client.SendTextMessageAsync(message.Chat, FileName + " добавлен в " + user.CurrentDir);
            }
            else
            {
                await client.SendTextMessageAsync(message.Chat, "У вас кончилось место или Вы не в хранилище!(Посмотреть сколько осталось места можно а Акаунте)");
                await client.DeleteMessageAsync(message.Chat, message.MessageId);
            }
            MoveTo(client, message, sql, CurrentUser.CurrentDir);
        }

        internal async static void AddInStorageSticker(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            Lib.User CurrentUser= sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
            if (CurrentUser.Step == 5 && user.CountItems != 0)
            {
                string FileName = DateTime.Now.ToString() + ".sticker";
                sql.Execute("insert into Files " +
                    "(FileId,FileName,FilePath,OwerId,Type) " +
                    "values(@fi,@fn,@fp,@oi,@tp)",
                    new
                    {
                        fi = message.Sticker.FileId,
                        fn = FileName,
                        oi = user.Id,
                        fp = CurrentUser.CurrentDir,
                        tp = "Sticker"
                    });
                if (user.PayStatus < 4)
                {
                    sql.Execute("update Users set " +
                   "CountItems=@ci where TelegramId=@ti",
                   new
                   {
                       ti = CurrentUser.CurrentAccountId,
                       ci = user.CountItems - 1
                   });
                }
                await client.SendTextMessageAsync(message.Chat,FileName + " добавлен в " + user.CurrentDir);
            }
            else
            {
                await client.SendTextMessageAsync(message.Chat, "У вас кончилось место или Вы не в хранилище!(Посмотреть сколько осталось места можно а Акаунте)");
                await client.DeleteMessageAsync(message.Chat, message.MessageId);
            }
            MoveTo(client, message, sql, CurrentUser.CurrentDir);
        }

        internal async static void AddInStorageDocument(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
            if (CurrentUser.Step == 5 && user.CountItems != 0)
            {
                string FileName = DateTime.Now.ToString() + ".document";
                if (message.Document.FileName!=null)
                {
                    FileName = message.Document.FileName;
                }
                sql.Execute("insert into Files " +
                    "(FileId,FileName,FilePath,OwerId,Type) " +
                    "values(@fi,@fn,@fp,@oi,@tp)",
                    new
                    {
                        fi = message.Document.FileId,
                        fn = FileName,
                        oi = user.Id,
                        fp = CurrentUser.CurrentDir,
                        tp = "Document"
                    });
                if (user.PayStatus < 4)
                {
                    sql.Execute("update Users set " +
                   "CountItems=@ci where TelegramId=@ti",
                   new
                   {
                       ti = CurrentUser.CurrentAccountId,
                       ci = user.CountItems - 1
                   });
                }
                await client.SendTextMessageAsync(message.Chat, FileName + " добавлен в " + user.CurrentDir);
            }
            else
            {
                await client.SendTextMessageAsync(message.Chat, "У вас кончилось место или Вы не в хранилище!(Посмотреть сколько осталось места можно а Акаунте)");
                await client.DeleteMessageAsync(message.Chat, message.MessageId);
            }
            MoveTo(client, message, sql, CurrentUser.CurrentDir);
        }

        internal async static void AddInStorageVideoNote(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
            if (CurrentUser.Step == 5 && user.CountItems != 0)
            {
                string FileName = DateTime.Now.ToString() + ".кружок";
                sql.Execute("insert into Files " +
                    "(FileId,FileName,FilePath,OwerId,Type) " +
                    "values(@fi,@fn,@fp,@oi,@tp)",
                    new
                    {
                        fi = message.VideoNote.FileId,
                        fn = FileName,
                        oi = user.Id,
                        fp = CurrentUser.CurrentDir,
                        tp = "VideoNote"
                    });
                if (user.PayStatus < 4)
                {
                    sql.Execute("update Users set " +
                   "CountItems=@ci where TelegramId=@ti",
                   new
                   {
                       ti = CurrentUser.CurrentAccountId,
                       ci = user.CountItems - 1
                   });
                }
                await client.SendTextMessageAsync(message.Chat, FileName + " добавлен в " + user.CurrentDir);
            }
            else
            {
                await client.SendTextMessageAsync(message.Chat, "У вас кончилось место или Вы не в хранилище!(Посмотреть сколько осталось места можно а Акаунте)");
                await client.DeleteMessageAsync(message.Chat, message.MessageId);
            }
            MoveTo(client, message, sql, CurrentUser.CurrentDir);
        }

        internal async static void AddInStoragePhoto(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
            Lib.Access access = null;
         
            if (CurrentUser.Step == 5 && user.CountItems != 0)
            {
                string FileName = DateTime.Now.ToString() + ".photo";
                sql.Execute("insert into Files " +
                    "(FileId,FileName,FilePath,OwerId,Type) " +
                    "values(@fi,@fn,@fp,@oi,@tp)",
                    new
                    {
                        fi = message.Photo[0].FileId,
                        fn = FileName,
                        oi = user.Id,
                        fp = CurrentUser.CurrentDir,
                        tp = "Photo"
                    });
                if (user.PayStatus < 4)
                {
                    sql.Execute("update Users set " +
                   "CountItems=@ci where TelegramId=@ti",
                   new
                   {
                       ti = CurrentUser.CurrentAccountId,
                       ci = user.CountItems - 1
                   });
                }
                await client.SendTextMessageAsync(message.Chat, FileName + " добавлен в " + user.CurrentDir);
            }
            else
            {
                await client.SendTextMessageAsync(message.Chat, "У вас кончилось место или Вы не в хранилище!(Посмотреть сколько осталось места можно а Акаунте)");
                await client.DeleteMessageAsync(message.Chat, message.MessageId);
            }
            MoveTo(client, message, sql, CurrentUser.CurrentDir);
        }

        internal async static void AddInStorageVoice(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
            if (CurrentUser.Step == 5 && user.CountItems != 0)
            {
                string FileName = DateTime.Now.ToString() + ".voice";
                sql.Execute("insert into Files " +
                    "(FileId,FileName,FilePath,OwerId,Type) " +
                    "values(@fi,@fn,@fp,@oi,@tp)",
                    new
                    {
                        fi = message.Voice.FileId,
                        fn = FileName,
                        oi = user.Id,
                        fp = CurrentUser.CurrentDir,
                        tp = "Voice"
                    });
                if (user.PayStatus < 4)
                {
                    sql.Execute("update Users set " +
                   "CountItems=@ci where TelegramId=@ti",
                   new
                   {
                       ti = CurrentUser.CurrentAccountId,
                       ci = user.CountItems - 1
                   });
                }
                await client.SendTextMessageAsync(message.Chat, FileName + " добавлен в " + user.CurrentDir);
            }
            else
            {
                await client.SendTextMessageAsync(message.Chat, "У вас кончилось место или Вы не в хранилище!(Посмотреть сколько осталось места можно а Акаунте)");
                await client.DeleteMessageAsync(message.Chat, message.MessageId);
            }
            MoveTo(client, message, sql, CurrentUser.CurrentDir);
        }

        public static async void Shop(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            try
            {
                sql.Execute("Update Users " +
                "set Step=@st where TelegramId=@ti", new { st = 1, ti = message.From.Id });
                ReplyKeyboardMarkup replyMarkup = new[] {
                            new[] { "Коины","Подписка" },
                            new[] { "Главная" }
                            };
                await client.SendTextMessageAsync(message.Chat, ">", replyMarkup: replyMarkup);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            
        }
        public static async void Main(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            try
            {
                sql.Execute("Update Users " +
                "set Step=@st where TelegramId=@ti", new { st = 0, ti = message.From.Id });
                ReplyKeyboardMarkup replyMarkup = new[] {
                            new[] { "Магазин","Акаунт" },
                            new[] { "Хранилище" }
                           };
                await client.SendTextMessageAsync(message.Chat, "<", replyMarkup: replyMarkup);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            
        }
        public static async void Account(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            try
            {
                Lib.User user = null;
                Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
                ReplyKeyboardMarkup replyMarkup = new[] {
                    new[] { "Переключить акаунт" },
                    new[] { "Предоставить доступ" },
                    new[] { "Убрать доступ" },
                            new[] { "Главная" },
                           };
                ReplyKeyboardMarkup replyMarkup1 = new[] {
                     new[] { "Переключить акаунт" },
                            new[] { "Главная" },
                           };
                if (CurrentUser.CurrentAccountId==message.From.Id)
                {
                    user = CurrentUser;
                    await client.SendTextMessageAsync(message.Chat, sql.QueryFirstOrDefault<Lib.User>("select FirstName,Username,TelegramId,Coins,CountItems,PayStatus " +
                   "from Users " +
                   "where TelegramId=@p", new { p = user.TelegramId }).ToString(), replyMarkup: replyMarkup);
                }
                else
                {
                    user= sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
                    await client.SendTextMessageAsync(message.Chat, sql.QueryFirstOrDefault<Lib.User>("select FirstName,Username,TelegramId,Coins,CountItems,PayStatus " +
                  "from Users " +
                  "where TelegramId=@p", new { p = user.TelegramId }).ToString(),replyMarkup:replyMarkup1);
                }
                sql.Execute("Update Users " +
              "set Step=@st where TelegramId=@ti", new { st = 4, ti = message.From.Id });
               
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
          
        }
       
        public async static void CoinsShop(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            try
            {
                List<string> prices = new List<string> { "10с - 1$", "50с - 4$", "200с - 16$", "500с - 49$", "1000с - 99$" };
                if (sql.QueryFirstOrDefault<Lib.User>("select Step from Users where TelegramId=@ti", new { ti = message.From.Id }).Step != 2)
                {
                    sql.Execute("Update Users " +
                    "set Step=@st where TelegramId=@ti", new { st = 2, ti = message.From.Id });
                    ReplyKeyboardMarkup replyMarkup = new[] {
                            new[] {prices[0] },
                           new[] {prices[1]  },
                            new[] {prices[2] },
                             new[] { prices[3] },
                              new[] { prices[4] },
                               new[] { "Главная" }
                           };
                    await client.SendTextMessageAsync(message.Chat, @"\/", replyMarkup: replyMarkup);
                }
                else
                {
                    for (int i = 0; i < prices.Count; i++)
                    {
                        if (prices[i] == message.Text)
                        {
                            int len = 0;
                            if (i < 2)
                            {
                                len = 1;
                            }
                            else
                            {
                                len = 2;
                            }
                            string pricestr = prices[i].Substring(prices[i].IndexOf("-") + 2, len);
                            int price = int.Parse(pricestr);
                            price *= 100;
                            Pay(client, message, price, prices[i]);

                        }
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
           


        }
        public  static void CheckStep(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            try
            {
                Lib.User user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
                int step = user.Step;
                int substep = 0;
                bool ok = false;
                if (step >= 10)
                {
                    substep = step % 10;
                    step /= 10;
                    ok = true;
                }
                switch (step)
                {
                    case 0: Main(client, message, sql); break;
                    case 1: Shop(client, message, sql); break;
                    case 2: CoinsShop(client, message, sql); break;
                    case 3: Sub(client, message, sql); break;
                    case 4:
                        if (ok)
                        {
                            switch (substep)
                            {
                                case 1:ConnectToAccount(client, message, sql);break;
                                case 2: GiveAccess(client, message, sql); break;
                                case 3: TakeAccess(client, message, sql);break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            AccountCommands(client, message, sql);
                        }
                       
                        
                        break;
                    case 5:
                        if (ok)
                        {
                            CommandsInCloud(client, message, sql, substep); break;
                        }
                        else
                        {
                            MoveTo(client, message, sql, message.Text); break;
                        }

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            
        }

        private static async void TakeAccess(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            try
            {
                switch (message.Text)
                {
                    case "Все":
                        await sql.ExecuteAsync("delete from Access where MainUserId=@mui", new { mui = message.From.Id }); await client.SendTextMessageAsync(message.Chat, "Все удалены");
                        Account(client, message, sql);
                        break;
                    case "Общий":
                        await sql.ExecuteAsync("delete from Access where MainUserId=@mui and General='true'", new { mui = message.From.Id });
                        await client.SendTextMessageAsync(message.Chat, "Общий доступ удалён");
                        Account(client, message, sql);
                        break;
                    default:
                        Lib.User user = sql.QueryFirstOrDefaultAsync<Lib.User>("select * from Users where Username=@un", new { un = message.Text.Substring(1) }).Result;
                        if (user != null)
                        {
                            await sql.ExecuteAsync("delete from Access where MainUserId=@mui and RefUserId=@rui", new { mui = message.From.Id, rui = user.TelegramId });
                            await client.SendTextMessageAsync(message.Chat, "Доступ для пользователя " + user.FirstName + " - @" + user.Username + " удалён");
                            Account(client, message, sql);
                        }
                        break;
                }
                Account(client, message, sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        private static async void GiveAccess(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            try
            {
                Lib.User MainUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
                if (MainUser.TelegramId != MainUser.CurrentAccountId)
                {
                    await client.SendTextMessageAsync(message.Chat, "Вы не можете предоставлять доступ с чужого акаунта!");
                    return;
                }
                string Username = "";
                bool Rule1 = false;
                bool General = false;


                if (message.Text != "Общий")
                {
                    Username = message.Text.Substring(0, message.Text.IndexOf(' '));
                    Rule1 = bool.Parse(message.Text.Substring(message.Text.IndexOf(' '), message.Text.Length - message.Text.IndexOf(' ')));
                }
                else
                {
                    if (sql.QueryFirstOrDefault<Lib.Access>("select * from Access where MainUserId=@ti and RefUserId=@ti", new { ti = message.From.Id }) == null)
                    {
                        General = true;
                        sql.Execute("insert into Access  (MainUserId,RefUserId,General,RuleUser) values(@mui,@rui,@gn,@rl)",
                        new
                        {
                            mui = MainUser.TelegramId,
                            rui = MainUser.TelegramId,
                            gn = General,
                            rl = Rule1
                        });
                        await client.SendTextMessageAsync(message.Chat, "Общий доступ придоставлен");
                        Account(client, message, sql);

                    }
                    else
                    {
                        await client.SendTextMessageAsync(message.Chat, "Общий доступ уже есть");
                    }
                    return;
                }
                Lib.User RefUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where Username=@un",
                    new { un = Username });

                if (RefUser == null)
                {
                    await client.SendTextMessageAsync(message.Chat, "Неверний тег или юзер не заригистрирован в Telegram Cloud");
                    Account(client, message, sql);
                    return;
                }
                Access access;
                if ((access = sql.QueryFirstOrDefault<Lib.Access>("select * from Access where MainUserId=@mti and RefUserId=@rui", new { mti = MainUser.TelegramId, rui = RefUser.TelegramId })) == null)
                {
                    sql.Execute("insert into Access  (MainUserId,RefUserId,General,RuleUser) values(@mui,@rui,@gn,@rl)",
                    new
                    {
                        mui = MainUser.TelegramId,
                        rui = RefUser.TelegramId,
                        gn = General,
                        rl = Rule1
                    });
                    await client.SendTextMessageAsync(message.Chat, "Доступ @" + RefUser.Username + " предоставлен!");
                    Account(client, message, sql);
                }
                else
                {
                    if (access.RuleUser != Rule1)
                    {
                        sql.Execute("Update Access set RuleUser=@ru where MainUserId=@mui and RefUserId=@rui",
                   new
                   {
                       mui = MainUser.TelegramId,
                       rui = RefUser.TelegramId,
                       ru = Rule1
                   });
                        await client.SendTextMessageAsync(message.Chat, "Доступ @" + RefUser.Username + " предоставлен!");
                        Account(client, message, sql);
                    }
                    else
                    {
                        await client.SendTextMessageAsync(message.Chat, "Доступ этому пользователю уже предоставлен");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            
        }

        public static async void ConnectToAccount(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            Lib.User RefUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            if (message.Text==RefUser.Username)
            {
                sql.Execute("update Users set CurrentAccountId=@cai where TelegramId=@ti", new { ti = RefUser.TelegramId, cai = RefUser.TelegramId });
                await client.SendTextMessageAsync(message.Chat, "Успешно переключены на акаунт " + RefUser.FirstName + " - @" + RefUser.Username);
                Main(client, message, sql);
                return;
            }
            Lib.User MainUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where Username=@un", new { un=message.Text });
            if (MainUser == null)
            {
                await client.SendTextMessageAsync(message.Chat, "Неверний тег или юзер не заригистрирован в Telegram Cloud");
                return;
            }
            Lib.Access access = sql.QueryFirstOrDefault<Lib.Access>("select * from Access where MainUserId=@mui and RefUserId=@rui or MainUserId=@mui and General='true'", new { mui = MainUser.TelegramId, rui = RefUser.TelegramId });
            if (access==null)
            {
                await client.SendTextMessageAsync(message.Chat, "Пользователь не предоставил доступ");
                Account(client, message, sql);
                return;
            }
            sql.Execute("update Users set CurrentAccountId=@cai where TelegramId=@ti", new { ti=message.From.Id,cai=MainUser.TelegramId });
            await client.SendTextMessageAsync(message.Chat, "Успешно переключены на акаунт " + MainUser.FirstName + " - @" + MainUser.Username);
            Main(client,message,sql);
        }

        private static async void AccountCommands(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            Lib.User user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            switch (message.Text)
            {
                case "Переключить акаунт":
                    ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new KeyboardButton("Назад"));
                    await client.SendTextMessageAsync(message.Chat, "Введите тег акаунта на которий хотите переключиться(Username)",replyMarkup:replyKeyboardMarkup);
                    sql.Execute("update Users set Step=@st where TelegramId=@ti", new { st = 41, ti = user.TelegramId });
                    break;
                case "Предоставить доступ":
                    if (user.TelegramId!=user.CurrentAccountId)
                    {
                        await client.SendTextMessageAsync(message.Chat, "Вы не можете предоставлять доступ с чужого акаунта!");
                        return;
                    }
                    ReplyKeyboardMarkup replyKeyboardMarkup1 = new ReplyKeyboardMarkup(new[] 
                    { 
                        new[] { new KeyboardButton("Общий") }, 
                        new[] { new KeyboardButton("Назад") }, 
                    });
                    await client.SendTextMessageAsync(message.Chat, "Общий предоставит доступ всем кто переключаться на твой акаунт" +
                        "\nИли введите тег кому хотите предоставить доступ и права" +
                        "\ntrue - может читать и редактировать хранилище" +
                        "\nfalse - только просматривать что в хранилище  (Username - права(true,false))", replyMarkup: replyKeyboardMarkup1);
                    sql.Execute("update Users set Step=@st where TelegramId=@ti", new { st = 42, ti = user.TelegramId });
                    break;
                case "Убрать доступ":
                    List<Lib.Access> accessList = new List<Lib.Access>();
                    if ((accessList= ChekerMessageUpdate.AccessGeter(client,message,sql).Result)==null)
                    {
                        await client.SendTextMessageAsync(message.Chat, "У вас нету окритых доступов");
                        return;
                    }
                    List<List<Lib.Access>> accesses = new List<List<Access>>();
                    bool General = false;
                    string msg = "";
                    foreach (var item in accessList)
                    {
                        if (item.General==true)
                        {
                            General = true;
                        }
                        else
                        {
                            Lib.User user1 = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = item.RefUserId });
                            msg += user1.FirstName + " - /" + user1.Username + " - "+item.RuleUser.ToString()+"\n";
                        }

                    }
                    ReplyKeyboardMarkup replyKeyboardMarkup12 = null;
                    if (General)
                    {
                        replyKeyboardMarkup12 = new ReplyKeyboardMarkup(new[]
                    {
                            new[] { new KeyboardButton("Все") },
                        new[] { new KeyboardButton("Общий") },
                        new[] { new KeyboardButton("Назад") },
                    });
                    }
                    else
                    {
                        replyKeyboardMarkup12 = new ReplyKeyboardMarkup(new[]
                    {
                            new[] { new KeyboardButton("Все") },
                        new[] { new KeyboardButton("Назад") },
                    });
                    }
                    await client.SendTextMessageAsync(message.Chat, "Выбирите один из вариантов или пользователя из списка:\n"+msg, replyMarkup: replyKeyboardMarkup12);
                   await sql.ExecuteAsync("update  Users set Step=@st where TelegramId=@ti", new { st = 43, ti = message.From.Id });
                    break;
                default:
                    break;
            }
        }

        public async static void Pay(ITelegramBotClient client,Message message,int price,string priceDescript)
        {
            await client.SendInvoiceAsync(chatId: message.Chat.Id,
              title: "Coins",
              description: "Внутренняя валюта",
              payload: "my-payload",
              providerToken: "284685063:TEST:MDRhNDFjNDU0YzRj",
              currency: "USD",
              photoUrl: "https://upload.wikimedia.org/wikipedia/commons/2/23/Russian_Empire-1899-Coin-5-Obverse.jpg",
              photoWidth: 1264,
              photoHeight: 1264,
              needShippingAddress: true,
              isFlexible: true,
              needName: true,
              maxTipAmount: 10,
              protectContent: true,
              prices: new List<LabeledPrice> { new LabeledPrice(priceDescript, price) }

                         );
        }

        public async static void Sub(ITelegramBotClient client,Message message,SqlConnection sql)
        {
            try
            {
                List<Lib.Price> prices = new List<Lib.Price> {
                new Lib.Price() {
                    Descriotion="Подписка: базовая\n" +
                "Период: 1нед\n" +
                "Стоимость: 15с\n" +
                "Количество файлов: 100",
                    _Price=15,
                    ShortInfo="Базовая - 15с"
                },
                 new Lib.Price() {
                    Descriotion="Подписка: умная\n" +
                "Период: 1мес\n" +
                "Стоимость: 74с\n" +
                "Количество файлов: 800",
                    _Price=74,
                    ShortInfo="Умная - 74с"
                },
                  new Lib.Price() {
                    Descriotion="Подписка: Vip\n" +
                "Период: 1год\n" +
                "Стоимость: 887с\n" +
                "Количество файлов: 2000",
                    _Price=887,
                    ShortInfo="Vip - 887с"
                },
                 new Lib.Price() {
                    Descriotion="Подписка: Vip-Plus\n" +
                "Период: Навсегда\n" +
                "Стоимость: 1000с\n" +
                "Количество файлов: 10000",
                    _Price=1000,
                    ShortInfo="Vip-Plus - 1000с"
                },
               new Lib.Price() {
                    Descriotion="Подписка: Vip-Max\n" +
                "Период: Навсегда\n" +
                "Стоимость: 1500с\n" +
                "Количество файлов: Без ограничений",
                    _Price=1500,
                    ShortInfo="Vip-Max - 1500с"
                }
            };
                if (sql.QueryFirstOrDefault<Lib.User>("select Step from Users where TelegramId=@ti", new { ti = message.From.Id }).Step != 3)
                {
                    sql.Execute("Update Users " +
                    "set Step=@st where TelegramId=@ti", new { st = 3, ti = message.From.Id });
                    ReplyKeyboardMarkup replyMarkup = new[] {
                            new[] {prices[0].ShortInfo },
                           new[] {prices[1].ShortInfo  },
                            new[] {prices[2].ShortInfo },
                             new[] { prices[3].ShortInfo },
                              new[] { prices[4].ShortInfo },
                               new[] { "Главная" }
                           };
                    await client.SendTextMessageAsync(message.Chat, @"\/", replyMarkup: replyMarkup);
                }
                else
                {
                    for (int i = 0; i < prices.Count; i++)
                    {
                        if (prices[i].ShortInfo == message.Text)
                        {
                            ReplyKeyboardMarkup replyMarkup = new[] {
                            new[] {"Купить" },
                               new[] { "Назад" }
                           };
                            sql.Execute("Update Users " +
                    "set Step=@st where TelegramId=@ti", new { st = 3 * 10 + i, ti = message.From.Id });
                            await client.SendTextMessageAsync(message.Chat, prices[i].Descriotion, replyMarkup: replyMarkup);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            
        }
        public  static void Back(ITelegramBotClient client,Message message,SqlConnection sql)
        {
            try
            {
                Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users " +
                "where TelegramId=@ti", new { ti = message.From.Id });
                Lib.User user = sql.QueryFirstOrDefault<Lib.User>("select * from Users " +
                "where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
                int newstep = 0;
                if (CurrentUser.Step < 10)
                {
                    newstep = CurrentUser.Step - 1;
                }
                else
                {
                    newstep = CurrentUser.Step / 10 - 1;
                }
                sql.Execute("Update Users " +
                   "set Step=@st where TelegramId=@ti", new { st = newstep, ti = CurrentUser.TelegramId});
                switch (newstep)
                {
                    case -1: Main(client, message, sql); break;
                    case 0: Shop(client, message, sql); break;
                    case 1: CoinsShop(client, message, sql); break;
                    case 2: Sub(client, message, sql); break;
                    case 3: Account(client, message, sql); break;
                    case 4:
                        if (CurrentUser.Step == 5)
                        {
                            var Path = sql.QueryFirstOrDefault<Lib.User>("select CurrentDir " +
                       "from Users " +
                       "where TelegramId=@ti",
                       new { ti = message.From.Id }).CurrentDir.Split('/');
                            string NormalPath = "";
                            for (int i = 0; i < Path.Length - 2; i++)
                            {
                                NormalPath += Path[i] + "/";
                            }
                            MoveTo(client, message, sql, NormalPath);
                        }
                        else
                        {
                            MoveTo(client, message, sql, CurrentUser.CurrentDir);
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            
           
        }

        public static async void Buy(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            try
            {
                List<Lib.Price> prices = new List<Lib.Price> {
                new Lib.Price() {
                    Descriotion="Подписка: базовая\n" +
                "Период: 1нед\n" +
                "Стоимость: 15с\nт" +
                "Количество файлов: 100",
                    _Price=15,
                    ShortInfo="Базовая - 15с"
                },
                 new Lib.Price() {
                    Descriotion="Подписка: умная\n" +
                "Период: 1мес\n" +
                "Стоимость: 74с\nт" +
                "Количество файлов: 800",
                    _Price=74,
                    ShortInfo="Умная - 74с"
                },
                  new Lib.Price() {
                    Descriotion="Подписка: Vip\n" +
                "Период: 1год\n" +
                "Стоимость: 887с\nт" +
                "Количество файлов: 2000",
                    _Price=887,
                    ShortInfo="Vip - 887с"
                },
                 new Lib.Price() {
                    Descriotion="Подписка: Vip-Plus\n" +
                "Период: Навсегда\n" +
                "Стоимость: 1000с\nт" +
                "Количество файлов: 10000",
                    _Price=1000,
                    ShortInfo="Vip-Plus - 1000с"
                },
               new Lib.Price() {
                    Descriotion="Подписка: Vip-Max\n" +
                "Период: Навсегда\n" +
                "Стоимость: 1500с\nт" +
                "Количество файлов: Без ограничений",
                    _Price=1500,
                    ShortInfo="Vip-Max - 1500с"
                }
            };
                Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users " +
                    "where TelegramId=@ti", new { ti = message.From.Id });
                Lib.User user = null;
                if (CurrentUser.CurrentAccountId==message.From.Id)
                {
                    user = CurrentUser;
                }
                else
                {
                    user= sql.QueryFirstOrDefault<Lib.User>("select * from Users " +
                    "where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId});
                }
                switch (CurrentUser.Step / 10)
                {
                    case 3:
                        if (user.Coins - prices[CurrentUser.Step % 10]._Price >= 0)
                        {
                            switch (CurrentUser.Step % 10)
                            {
                                case 0:
                                    sql.Execute("update Users " +
                             "Set Coins=@p,PayStatus=@ps,CountItems=@ci,PayDateLeft=@pdl where TelegramId=@ti", new { pdl = DateTime.Now.AddDays(7), ti = user.TelegramId, p = user.Coins - prices[CurrentUser.Step % 10]._Price, ps = 0, ci = 100 }); break;
                                case 1:
                                    sql.Execute("update Users " +
                             "Set Coins=@p,PayStatus=@ps,CountItems=@ci,PayDateLeft=@pdl where TelegramId=@ti", new { pdl = DateTime.Now.AddMonths(1), ti = user.TelegramId, p = user.Coins - prices[CurrentUser.Step % 10]._Price, ps = 1, ci = 800 }); break;
                                case 2:
                                    sql.Execute("update Users " +
                             "Set Coins=@p,PayStatus=@ps,CountItems=@ci,PayDateLeft=@pdl where TelegramId=@ti", new { pdl = DateTime.Now.AddYears(1), ti = user.TelegramId, p = user.Coins - prices[CurrentUser.Step % 10]._Price, ps = 2, ci = 2000 }); break;
                                case 3:
                                    sql.Execute("update Users " +
                             "Set Coins=@p,PayStatus=@ps,CountItems=@ci,PayDateLeft=@pdl where TelegramId=@ti", new { pdl = DateTime.Now.AddYears(100), ti = user.TelegramId, p = user.Coins - prices[CurrentUser.Step % 10]._Price, ps = 3, ci = 10000 }); break;
                                case 4:
                                    sql.Execute("update Users " +
                             "Set Coins=@p,PayStatus=@ps,CountItems=@ci,PayDateLeft=@pdl where TelegramId=@ti", new { pdl = DateTime.Now.AddYears(100), ti = user.TelegramId, p = user.Coins - prices[CurrentUser.Step % 10]._Price, ps = 4, ci = 9999999 }); break;
                                default:
                                    break;
                            }
                            await client.SendTextMessageAsync(message.Chat, "Успешно!");
                            Main(client, message, sql);

                        }
                        else
                        {
                            await client.SendTextMessageAsync(message.Chat, "Недостаточно коинов!");
                            CoinsShop(client, message, sql);
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            

        }

        public static  void Storage(ITelegramBotClient client,Message message,SqlConnection sql)
        {
            MoveTo(client, message, sql,"Storage/");
        }
         public static async void MoveTo(ITelegramBotClient client, Message message, SqlConnection sql,string Path1)
        {
            try
            {
                Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * " +
              "from Users where " +
              "TelegramId=@ti",
              new { ti = message.From.Id });
                Lib.User user = sql.QueryFirstOrDefault<Lib.User>("select Id,CurrentDir,Step " +
               "from Users where " +
               "TelegramId=@ti",
               new { ti = CurrentUser.CurrentAccountId });
                string buffer = Path1;
                string Path;

                Path = Path1;


                switch (Path)
                {
                    case "Создать папку":
                        ReplyKeyboardMarkup keyboardMarkup = new ReplyKeyboardMarkup(new KeyboardButton("Назад"));
                        sql.Execute("update Users " +
                           "set Step=@st " +
                           "where TelegramId=@ti",
                           new
                           {
                               ti = message.From.Id,
                               st = 51
                           }
                           );
                        await client.SendTextMessageAsync(message.Chat, "Введите имя папки", replyMarkup: keyboardMarkup);
                        return;
                    case "Удалить папку":
                        ReplyKeyboardMarkup keyboardMarkupDelete = new ReplyKeyboardMarkup(new[]{
                       new[]{ new KeyboardButton("ОК") },
                       new[]{ new KeyboardButton("Назад") }
                    });
                        sql.Execute("update Users " +
                           "set Step=@st " +
                           "where TelegramId=@ti",
                           new
                           {
                               ti = message.From.Id,
                               st = 52
                           }
                           );
                        await client.SendTextMessageAsync(message.Chat, "Все файли и подпапки будут удалени\nТочно хотите удалить папку?", replyMarkup: keyboardMarkupDelete);
                        return;
                    default:
                        break;
                }
                List<Lib.File> files = null;
                Lib.File file = null;
                bool ok = false;

                if (buffer.IndexOf("Storage") == -1)
                {
                    Path = CurrentUser.CurrentDir + buffer + "/";
                }

                if (Path.IndexOf('.') == -1)
                {
                    files = sql.Query<Lib.File>("select FileId,FileName,FilePath,OwerId " +
                   "from Files where " +
                   "FilePath=@fp and OwerId=@oi",
                   new { fp = Path, oi = user.Id }).ToList();
                }
                else
                {
                    file = sql.QueryFirstOrDefault<Lib.File>("select FileId,FileName,FilePath,OwerId,Type " +
                  "from Files where " +
                  "FilePath=@fp and OwerId=@oi and FileName=@fn",
                  new { fp = CurrentUser.CurrentDir, oi = user.Id, fn = buffer });
                    ok = true;
                }

                if (Path1 == "Storage/")
                {
                    Path = Path1;
                }



                if (ok)
                {
                    ReplyKeyboardMarkup replyMarkup1 = new[] {
                            new[] {"Удалить" },
                             new[] {"Переименовать" },
                           new[] {"Назад" },
                           };
                    sql.Execute("update Users " +
                        "set SelectedFileId=@sfi,Step=@st " +
                        "where TelegramId=@ti",
                        new
                        {
                            sfi = file.FileId,
                            ti = message.From.Id,
                            st = CurrentUser.Step * 10
                        }
                        );
                    await client.SendTextMessageAsync(message.Chat, @"/\", replyMarkup: replyMarkup1);
                    SendFile(client, message, file, sql);
                    
                    return;
                }
                sql.Execute("update Users " +
                    "set Step=@st,CurrentDir=@cd,SelectedFileId=@sfi " +
                    "where TelegramId=@ti",
                    new
                    {
                        ti = message.From.Id,
                        st = 5,
                        cd = Path,
                        sfi = string.Empty
                    });
                List<List<KeyboardButton>> keyboardButtons = new List<List<KeyboardButton>>();
                keyboardButtons.Add(new List<KeyboardButton>() { new KeyboardButton("Главная") });
                if (Path != "Storage/")
                {
                    keyboardButtons.Add(new List<KeyboardButton>() { new KeyboardButton(@"Назад") });
                    keyboardButtons.Add(new List<KeyboardButton>() { new KeyboardButton("Создать папку") });
                    keyboardButtons[2].Add("Удалить папку");
                }
                else
                {
                    keyboardButtons.Add(new List<KeyboardButton>() { new KeyboardButton("Создать папку") });
                    keyboardButtons[1].Add("Удалить папку");
                }


                foreach (Lib.File file1 in files)
                {
                    keyboardButtons.Add(new List<KeyboardButton>() { new KeyboardButton(file1.FileName) });
                }
                ReplyKeyboardMarkup replyMarkup = new ReplyKeyboardMarkup(keyboardButtons);
                await client.SendTextMessageAsync(message.Chat, Path, replyMarkup: replyMarkup);

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            
        }

        public static async void AddInStorageVideo(ITelegramBotClient client,Message message,SqlConnection sql)
        {
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
            if (CurrentUser.Step == 5 && user.CountItems != 0)
            {
                string FileName = DateTime.Now.ToString() + ".video";
                if (message.Video.FileName != null)
                {
                    FileName = message.Video.FileName;
                }
                sql.Execute("insert into Files " +
                    "(FileId,FileName,FilePath,OwerId,Type) " +
                    "values(@fi,@fn,@fp,@oi,@tp)",
                    new
                    {
                        fi = message.Video.FileId,
                        fn = FileName,
                        oi = user.Id,
                        fp = CurrentUser.CurrentDir,
                        tp = "Video"
                    });
                if (user.PayStatus < 4)
                {
                    sql.Execute("update Users set " +
                   "CountItems=@ci where TelegramId=@ti",
                   new
                   {
                       ti = CurrentUser.CurrentAccountId,
                       ci = user.CountItems - 1
                   });
                }
                await client.SendTextMessageAsync(message.Chat, FileName + " добавлен в " + user.CurrentDir);
            }
            else
            {
                await client.SendTextMessageAsync(message.Chat, "У вас кончилось место или Вы не в хранилище!(Посмотреть сколько осталось места можно а Акаунте)");
                await client.DeleteMessageAsync(message.Chat, message.MessageId);
            }
            MoveTo(client, message, sql, CurrentUser.CurrentDir);
        }
        public static async void CommandsInCloud(ITelegramBotClient client,Message message,SqlConnection sql,int substep)
        {
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * " +
                    "from Users " +
                    "where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = sql.QueryFirstOrDefault<Lib.User>("select * " +
                    "from Users " +
                    "where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId});
            switch (message.Text)
            {
                case "Назад":Back(client, message, sql);break;
                case "Удалить":
                    if (ChekerMessageUpdate.Access(client,message,sql).Result)
                    {
                        sql.Execute("delete from Files " +
                    "where FileId=@fi"
                    , new
                    {
                        fi = CurrentUser.SelectedFileId
                    });
                        await client.SendTextMessageAsync(message.Chat, "Удалено!");
                        sql.Execute("update Users " +
                            "set CountItems=@ci where TelegramId=@ti",
                            new { ci = user.CountItems + 1, ti = CurrentUser.CurrentAccountId });
                        Back(client, message, sql);
                    }
                    
                        break;
                    
                case "Переименовать":
                    if (ChekerMessageUpdate.Access(client, message, sql).Result)
                    {
                        await client.SendTextMessageAsync(message.Chat, "Введите новое название файла");
                        sql.Execute("update Users set Step=@st where TelegramId=@ti", new { st = 53, ti = message.From.Id });
                    }
                   
                    break;
                default:
                    switch (substep)
                    {
                        case 1:
                            if (ChekerMessageUpdate.Access(client,message,sql).Result)
                            {
                                CreateFolder(client, message, sql); break;
                            }
                            break;
                        case 2:
                            if (ChekerMessageUpdate.Access(client,message,sql).Result&&message.Text=="ОК")
                            {
                                DeleteFolder(client, message, sql, user.CurrentDir);
                                Storage(client,message, sql);
                            }
                            break;
                        case 3:
                            if (ChekerMessageUpdate.Access(client,message,sql).Result)
                            {
                                RenameFile(client, message, sql);     
                            }
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }

        private static async void RenameFile(ITelegramBotClient client, Message message, SqlConnection sql)
        {
            if (message.Text.IndexOf('.')==-1)
            {
                await client.SendTextMessageAsync(message.Chat, "Напишите росширение файла(имя файла.росширение)");
                return;
            }
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * " +
                      "from Users " +
                      "where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = sql.QueryFirstOrDefault<Lib.User>("select * " +
                    "from Users " +
                    "where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId });
            if (user != null)
            {
                List<Lib.File> files = sql.Query<Lib.File>("select FileName from Files where OwerId=@oi and FilePath=@fp", new { oi = user.Id,fp=CurrentUser.CurrentDir }).ToList();
                foreach(Lib.File file in files)
                {
                    if (file.FileName==message.Text)
                    {
                        await client.SendTextMessageAsync(message.Chat, "Файл с таким именем уже есть!");
                        return;
                    }
                }
                sql.Execute("Update Files set FileName=@fn where OwerId=@oi and FilePath=@fp And FileId=@fi", new { oi = user.Id, fp = CurrentUser.CurrentDir,fn=message.Text,fi=CurrentUser.SelectedFileId });
                await client.SendTextMessageAsync(message.Chat, "Переименовано");
                MoveTo(client, message, sql,CurrentUser.CurrentDir);
            }
        }

        private static  void DeleteFolder(ITelegramBotClient client, Message message, SqlConnection sql,string Path)
        {
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = message.From.Id });
            Lib.User user = sql.QueryFirstOrDefault<Lib.User>("select * from Users where TelegramId=@ti", new { ti = CurrentUser.CurrentAccountId});
            List<Lib.File> files = sql.Query<Lib.File>("select * from Files where FilePath=@fp and OwerId=@oi", new {fp=Path,oi=user.Id}).ToList();
            int countDeletedFiles = 0;
            foreach (Lib.File file in files)
            {
                if (file.Type=="Folder")
                {
                    DeleteFolder(client, message, sql, file.FilePath+file.FileName+"/");
                    
                }
                sql.Execute("delete from Files where Id=@id and OwerId=@oi", new { id = file.Id,oi=user.Id,fp=file.FilePath});
                if (file.Type=="Folder")
                {
                     client.SendTextMessageAsync(message.Chat, "Папка " + file.FileName + " удалена");
                }
                else
                {
                     client.SendTextMessageAsync(message.Chat, "Файл " + file.FileName + " в папке "+file.FilePath+" удалён");
                    countDeletedFiles++;
                }
                sql.Execute("update Users Set CountItems=@ci where TelegramId=@ti", new {ti=CurrentUser.CurrentAccountId, ci = user.CountItems + countDeletedFiles });
               
            }
        }

        public static  void SendFile(ITelegramBotClient client,Message message,Lib.File file,SqlConnection sql)
        {
            switch (file.Type)
            {
                case "Video": client.SendVideoAsync(message.Chat, new Telegram.Bot.Types.InputFiles.InputOnlineFile(file.FileId));break;
                case "Audio": client.SendAudioAsync(message.Chat, new Telegram.Bot.Types.InputFiles.InputOnlineFile(file.FileId));break;
                case "Voice": client.SendVoiceAsync(message.Chat, new Telegram.Bot.Types.InputFiles.InputOnlineFile(file.FileId)); break;
                case "Photo": client.SendPhotoAsync(message.Chat, new Telegram.Bot.Types.InputFiles.InputOnlineFile(file.FileId)); break;
                case "Document": client.SendDocumentAsync(message.Chat, new Telegram.Bot.Types.InputFiles.InputOnlineFile(file.FileId)); break;
                case "Sticker": client.SendStickerAsync(message.Chat, new Telegram.Bot.Types.InputFiles.InputOnlineFile(file.FileId)); break;
                case "VideoNote": client.SendVideoNoteAsync(message.Chat, new Telegram.Bot.Types.InputFiles.InputOnlineFile(file.FileId)); break;
                default:
                    break;
            }
        }
        public static  async void CreateFolder(ITelegramBotClient client,Message message,SqlConnection sql)
        {
            Lib.User CurrentUser = sql.QueryFirstOrDefault<Lib.User>("select * " +
                "from Users " +
                "where TelegramId=@ti",
                new { ti = message.From.Id });
            Lib.User user = sql.QueryFirstOrDefault<Lib.User>("select * " +
               "from Users " +
               "where TelegramId=@ti",
               new { ti = CurrentUser.CurrentAccountId });
            if (user == null)
            {
                return;
            }
            List<Lib.File> files = sql.Query<Lib.File>("select FileName from Files where OwerId=@oi and Type='Folder' and FilePath=@fp", new { oi = user.Id,fp=CurrentUser.CurrentDir }).ToList();
            foreach (var item in files)
            {
                if (item.FileName==message.Text)
                {
                    await client.SendTextMessageAsync(message.Chat, "Имена папок не должны повторяться!");
                    Back(client, message, sql);
                    return;
                }
            }
            sql.Execute("insert into Files " +
                "(FileId,FileName,FilePath,OwerId,Type) " +
                "values(@fi,@fn,@fp,@oi,@tp)",
                new { fi = "Folder", fn = message.Text, fp = CurrentUser.CurrentDir, oi = user.Id, tp = "Folder" });
            MoveTo(client, message, sql, CurrentUser.CurrentDir);
            await client.SendTextMessageAsync(message.Chat, "Папка " + message.Text + " создана");
        }
       
    }
}

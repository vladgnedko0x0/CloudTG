using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Dapper;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json;

namespace Lib
{

    public class Price
    {
        public string Descriotion { get; set; }
        public int _Price { get; set; }
        public string ShortInfo { get; set; }

    }
    public class Access
    {
        public long MainUserId { get; set; }
        public long RefUserId { get; set; }
        public bool General { get; set; }
        public bool RuleUser { get; set; }

    }
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string Username { get; set; }
        public int CountItems { get; set; }
        public long TelegramId { get; set; }
        public int Coins { get; set; }
        public int Step { get; set; }
        public int PayStatus { get; set; }
        public string CurrentDir { get; set; }
        public string SelectedFileId { get; set; }
        public DateTime PayDateLeft { get; set; }
        public long CurrentAccountId { get; set; }
        public override string ToString()
        {
            string pay = "";
            switch (PayStatus)
            {
                case -1: pay = "Без подписки"; break;
                case 0: pay = "Базовая"; break;
                case 1: pay = "Умная"; break;
                case 2: pay = "Vip"; break;
                case 3: pay = "Vip_Plus"; break;
                case 4: pay = "Vip-Max"; CountItems = 999999999; break;
                default:
                    break;
            }
            return "Имя: " + FirstName + "\n" +
                "Имя пользователя: " + Username + "\n" +
                "ID: " + TelegramId + "\n" +
                "Осталось места: " + CountItems + " штук\n" +
                "Коины: " + Coins + "\n" +
                "Подписка: " + pay;
        }

    }
    public class File
    {
        public int Id { get; set; }
        public string FileId { get; set; }
        public string FileName { get; set; }
        public int OwerId { get; set; }
        public string Type { get; set; }
        public string FilePath { get; set; }
    }

}

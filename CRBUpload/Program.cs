using System.Data.SqlTypes;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Xml;
using static System.Net.WebRequestMethods;
using Npgsql;

class Program
{
    static void Main()
    {
        Console.WriteLine("Добрый день! Пожалуйста выберите действие из списка введя номер команды");
        Console.WriteLine("1. Вывести курс валют на сегодня");
        Console.WriteLine("2. Вывести курс валют за месяц");
        Console.WriteLine("3. Выход");

        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                string todayDate = DateTime.Today.ToString("dd/mm/yyyy");
                Console.WriteLine(UploadTodayQuotes(todayDate));
                break;
            case "2":
                var date = DateTime.Today;
                for(int i = 0; i < 30; i++)
                {
                    UploadTodayQuotes(date.ToString("dd/mm/yyyy"));
                    date.AddDays(-1);
                }
                break;
            case "3":
                Environment.Exit(0);
                break;
            default:
                Console.WriteLine("Неверный выбор. Пожалуйста, попробуйте снова.");
                break;
        }
    }

    public static string UploadTodayQuotes(string todayDate)
    {
        //daily currency
        int numCode;
        string charCode, name, value;
        string result = "Данные за " + todayDate + " загружены";

        string cotirovkasURL = "https://cbr.ru/scripts/XML_daily.asp?date_req=" + todayDate;

        XmlTextReader reader = new XmlTextReader(cotirovkasURL);

        string connString = "Host=localhost;Username=postgres;Password=postgres;Database=quotes";
        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            //read daily cotirovkas info, upload to the temporary variables to incorporate into sql command
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: 
                        if (reader.Name == "NumCode")
                            numCode = reader.Value.toInt();
                        if (reader.Name == "CharCode")
                            charCode = reader.Value;
                        if (reader.Name == "Name")
                            name = reader.Value;
                        if (reader.Name == "Value")
                            value = reader.Value;
                        break;
                    case XmlNodeType.EndElement:
                        if(reader.Name == "Valute")
                        {
                            // adding new information to the database
                            string sql = "INSERT INTO quotes VALUES (@id, @charcode, @name, @value, @numcode, @date)";
                            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                            {
                                command.Parameters.AddWithValue("@id", numCode + todayDate);
                                command.Parameters.AddWithValue("@charcode", charCode);
                                command.Parameters.AddWithValue("@name", name);
                                command.Parameters.AddWithValue("@value", value);
                                command.Parameters.AddWithValue("@date", DateTime.Today);

                                command.ExecuteNonQuery();
                            }
                        }
                        break;
                }
                
            }
        }

        return result;
    }
}
 
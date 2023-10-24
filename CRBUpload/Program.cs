using System.Data.SqlTypes;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Xml;
using static System.Net.WebRequestMethods;
using Npgsql;
using System.Text;
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Good Afternoon! Please select one of the further options by entering the corresponding number");
        Console.WriteLine("1. Add today quotes to the database");
        Console.WriteLine("2. Add monthly quotes to the database");
        Console.WriteLine("3. Exit");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                string todayDate = DateTime.Today.ToString("dd/mm/yyyy");
                Console.WriteLine(UploadTodayQuotes(todayDate));
                break;
            case "2":
                var date = DateTime.Today;
                for (int i = 0; i < 30; i++)
                {
                    UploadTodayQuotes(date.ToString("dd/mm/yyyy"));
                    date.AddDays(-1);
                }
                break;
            case "3":
                Environment.Exit(0);
                break;
            default:
                Console.WriteLine("Wrong choice. Please try again");
                break;
        }
    }

    public static string UploadTodayQuotes(string todayDate)
    {
        string result = "Data for " + todayDate + " has not been uploaded";

        string cotirovkasURL = "https://cbr.ru/scripts/XML_daily.asp?date_req=" + todayDate;

        // Download XML content from the URL
        string xmlContent;
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = client.GetAsync(cotirovkasURL).Result;
            xmlContent = response.Content.ReadAsStringAsync().Result;
        }

        // Convert Win-1251 encoded XML content to UTF-8
        Encoding win1251 = Encoding.GetEncoding("windows-1251");
        Encoding utf8 = Encoding.UTF8;
        byte[] utf8Bytes = Encoding.Convert(win1251, utf8, win1251.GetBytes(xmlContent));
        string utf8XmlContent = utf8.GetString(utf8Bytes);

        // Parse XML content
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(utf8XmlContent);


        string connString = "Host=localhost;Username=postgres;Password=postgres;Database=quotes";

        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            //read daily cotirovkas info, upload to the temporary variables to incorporate into sql command
            foreach (XmlNode node in xmlDoc.SelectNodes("//Valute"))
            {
                string numCode = node.SelectSingleNode("NumCode").InnerText;
                string charCode = node.SelectSingleNode("CharCode").InnerText;
                string name = node.SelectSingleNode("Name").InnerText;
                double value = Convert.ToDouble(node.SelectSingleNode("Value").InnerText, CultureInfo.InvariantCulture);

                // adding new information to the database
                string sql = "INSERT INTO quotes VALUES (@id, @charcode, @name, @value, @numcode, @date)";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", numCode + todayDate);
                    command.Parameters.AddWithValue("@charcode", charCode);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@value", value);
                    command.Parameters.AddWithValue("@date", todayDate);

                    command.ExecuteNonQuery();
                }
                result = "Data for " + todayDate + " has been uploaded";
            }
        }

        return result;
    }

}
 

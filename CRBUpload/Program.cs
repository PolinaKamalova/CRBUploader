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

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var date = DateTime.Today;
        for (int i = 0; i < 31; i++)
        {
            date = date.AddDays(-1);
            UploadTodayQuotes(date.ToString("dd/MM/yyyy"));
        }

        string todayDate = DateTime.Today.ToString("dd/MM/yyyy");
        Console.WriteLine(UploadTodayQuotes(todayDate));
    }

    public static string UploadTodayQuotes(string todayDate)
    {
        string result = "Data for " + todayDate + " has not been uploaded";

        string cotirovkasURL = "https://cbr.ru/scripts/XML_daily.asp?date_req="+todayDate;

        // Download XML content from the URL

        XmlDocument xmlDoc = new XmlDocument();
        try
        {
            string utf8XmlContent;
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(cotirovkasURL).Result;
                byte[] win1251Bytes = response.Content.ReadAsByteArrayAsync().Result;

                // Convert Win-1251 encoded XML content to UTF-8
                Encoding win1251 = Encoding.GetEncoding("windows-1251");
                Encoding utf8 = Encoding.UTF8;
                byte[] utf8Bytes = Encoding.Convert(win1251, utf8, win1251Bytes);
                utf8XmlContent = utf8.GetString(utf8Bytes);
            }

            // Parse XML content
            xmlDoc.LoadXml(utf8XmlContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while parsing XML: " + ex.Message);
        }


        string connString = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=quotes";

        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {

            connection.Open();

            string sqlCheck = "SELECT date FROM quotations WHERE date='" + todayDate + "' LIMIT 1;";
            using (NpgsqlCommand commandCheck = new NpgsqlCommand(sqlCheck, connection))
            {
                //check if needs to add info on daily quotes
                var check = Convert.ToString(commandCheck.ExecuteScalar());
                if (check.Equals(""))
                {
                    //read daily cotirovkas info, upload to the temporary variables to incorporate into sql command
                    foreach (XmlNode node in xmlDoc.SelectNodes("//ValCurs/Valute"))
                    {
                        string numCode = node.SelectSingleNode("NumCode").InnerText;
                        string charCode = node.SelectSingleNode("CharCode").InnerText;
                        string name = node.SelectSingleNode("Name").InnerText;
                        NumberFormatInfo provider = new NumberFormatInfo();
                        provider.NumberDecimalSeparator = ",";
                        double value = Convert.ToDouble(node.SelectSingleNode("VunitRate").InnerText, provider);
                        // adding new information to the database
                        string sql = "INSERT INTO quotations(id, charcode, name, value, numcode, date) VALUES (@id, @charcode, @name, @value, @numcode, @date)";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@id", numCode + todayDate);
                            command.Parameters.AddWithValue("@charcode", charCode);
                            command.Parameters.AddWithValue("@name", name);
                            command.Parameters.AddWithValue("@value", value);
                            command.Parameters.AddWithValue("@numcode", Convert.ToInt32(numCode));
                            command.Parameters.AddWithValue("@date", todayDate);


                           command.ExecuteNonQuery();
                        }

                        result = "Data for " + todayDate + " has been uploaded";
                    }
                }
            }
            connection.Close();
        }

        return result;
    }

}
 

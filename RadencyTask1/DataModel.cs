using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;

namespace RadencyTask1
{
    public class InputData : InputDataCSV
    {
        public string City { get; set; }
        /*
        public string Street { get; set; }
        public int HouseNumber { get; set; }
        public int AppartmentNumber { get; set; }
        */
    }
    public class InputDataCSV
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public decimal Payment { get; set; }
        public DateTime Date { get; set; }
        public long AccountNumber { get; set; }
        public string Service { get; set; }
    }
    abstract class AbstractNameble
    {
        public string Name { get; set; }
        protected static string Spaces(int n)
        {
            return new string(' ', n);
        }
        public virtual string getJSONString(int spaces = 0)
        {
            return "";
        }

        public virtual decimal CountTotal()
        {
            return 0;
        }
    }

    abstract class AbstractGroup<T> : AbstractNameble where T : AbstractNameble, new()
    {
        public List<T> list = new List<T>();
        public decimal Total { get; set; }

        public  T AddUnique(string name)
        {
            if (!list.Any(c => c.Name == name))
            {
                list.Add(new T { Name = name });
            }
            return list[list.Count-1];
        }

        public override string getJSONString(int spaces = 0)
        {
            string name = (typeof(T) == typeof(Service)) ? "city" : "name";
            string listName = (typeof(T) == typeof(Service)) ? "services" : "payers";

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(Spaces(spaces) + "{\n");
            stringBuilder.Append(Spaces(spaces + 1) + "\"" + name + "\": \"" + Name + "\",\n");
            stringBuilder.Append(Spaces(spaces + 1) + "\"" + listName + "\": \n");
            stringBuilder.Append(Spaces(spaces + 2) + "[\n");

            int count = 0;
            foreach (T item in list)
            {
                count++;
                stringBuilder.Append(item.getJSONString(spaces + 3))
                    .Append(((count != list.Count) ? "," : "") + "\n");
            }
            stringBuilder.Append(Spaces(spaces + 2) + "],\n");
            stringBuilder.Append(Spaces(spaces + 1) + "\"total\": " + Total.ToString(CultureInfo.InvariantCulture) + "\n");
            stringBuilder.Append(Spaces(spaces) + "}");

            return stringBuilder.ToString();
        }

        public override decimal CountTotal()
        {
            foreach (var item in list)
            {
                Total += item.CountTotal();
            }
            return Total;
        }
    }

    class Payer : AbstractNameble
    {
        public decimal Payment { get; set;}
        public DateTime Date { get; set; }
        public long AccountNumber { get; set; }

        
        public override string getJSONString(int spaces = 0)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(Spaces(spaces) + "{\n");
            stringBuilder.Append(Spaces(++spaces) + "\"name\": \"" + Name + "\",\n");
            stringBuilder.Append(Spaces(spaces) + "\"payment\": " + Payment.ToString(CultureInfo.InvariantCulture) + ",\n");
            stringBuilder.Append(Spaces(spaces) + "\"date\": \"" + Date.ToString("yyyy-dd-MM") + "\",\n");
            stringBuilder.Append(Spaces(spaces) + "\"account_number\": " + AccountNumber + "\n");
            stringBuilder.Append(Spaces(--spaces) + "}");

            return stringBuilder.ToString();
        }
        public override decimal CountTotal()
        {
            return Payment;
        }
    }

    class Service : AbstractGroup<Payer>
    {
        public void AddUnique(Payer payer)
        {
           
            var p1 = list.Find(p => p.Name == payer.Name && p.AccountNumber == payer.AccountNumber);
            if (p1 == null)
                list.Add(payer);
            else
                p1.Payment += payer.Payment;
        }
    }
    class City : AbstractGroup<Service>
    {
       
    }
    class AllData : AbstractGroup<City>
    {
        public AllData(List<InputData> inputData)
        {
            Name = "";
            foreach (var dataString in inputData)
            {
                AddUnique(dataString.City)
                .AddUnique(dataString.Service)
                .AddUnique(new Payer
                {
                    Name = dataString.FirstName + " " + dataString.LastName,
                    AccountNumber = dataString.AccountNumber,
                    Payment = dataString.Payment,
                    Date = dataString.Date
                });
            }
            CountTotal();
        }
        public string getJSONString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[\n");
            int count = 0;
            foreach (var city in list)
            {
                count++;
                stringBuilder.Append(city.getJSONString(1))
                    .Append(((count != list.Count) ? "," : "") + "\n");
            }
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }
        
    }
}

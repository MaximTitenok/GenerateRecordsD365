
using System;
using System.Collections.Generic;
using System.Net;
using System.Timers;
using System.Threading.Tasks;
using System.Security.Cryptography.Xml;
using Microsoft.Crm.Sdk.Messages;

namespace GenerateRentsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            EnterNumberRecords();
            Console.WriteLine("Data generation completed. Press any key to exit.");
            Console.ReadKey();
        }
        public static void EnterNumberRecords()
        {
            Console.WriteLine("Enter the number of new records(to 40.000):");
            if (!int.TryParse(Console.ReadLine(), out int numberOfRecords))
            {
                Console.WriteLine("Incorrect data!");
                EnterNumberRecords();
                return;
            }
            if (numberOfRecords > 40000)
            {
                Console.WriteLine("The number is over 40000! Repeat input!");
                EnterNumberRecords();
                return;
            }

            Console.WriteLine($"You entered the number: {numberOfRecords}. " +
                $"Are you confirm the adding? (y/n)");
            if (!char.TryParse(Console.ReadLine(), out char confirm))
            {
                Console.WriteLine("Incorrect data!");
                EnterNumberRecords();
                return;
            }
            if (confirm == 'y' || confirm == 'Y')
            {
                AddRecords addRecords = new AddRecords();
                var timer = new Timer(30000); 
                timer.Elapsed += timerElapsed;

                void timerElapsed(object sender, ElapsedEventArgs e)
                {
                    Console.WriteLine($"Records added: {addRecords.GetNumOfAddedRecords()}");
                    Console.WriteLine($"Car reports added: {addRecords.GetNumOfAddedReports()}"); ; // Устанавливаем обработчик события Elapsed
                }
                timer.Start();
                addRecords.AddRent(numberOfRecords);
                timer.Stop();
                if (addRecords.GetStatus())
                {
                    Console.WriteLine($"Completed! Records added: {addRecords.GetNumOfAddedRecords()}");
                    Console.WriteLine($"Completed! Car reports added: {addRecords.GetNumOfAddedReports()}");
                }
                else
                {
                    Console.WriteLine("Error: " + addRecords.GetError());
                    return;
                }
            }
            else
            {
                EnterNumberRecords();
            }   
        }

    }
}


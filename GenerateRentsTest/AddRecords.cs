using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;

namespace GenerateRentsTest
{
    internal class AddRecords
    {
        private const string CONNEСTION_STRING = "AuthType = Office365; Url = https://org183965c6.crm4.dynamics.com;" +
            "Username = api@maximtitenok.onmicrosoft.com; Password = keirDa1269";
        private bool Status { get; set; } = false;
        private string Error { get; set; } = string.Empty;
        private int NumOfAddedRecords { get; set; } = 0;
        private int NumOfAddedReports { get; set; } = 0;
        

        EntityCollection entityOfClasses = new EntityCollection();
        EntityCollection entityOfCars = new EntityCollection();
        EntityCollection entityOfCustomers = new EntityCollection();
        EntityCollection entityOfReports = new EntityCollection();

        QueryExpression queryClasses = new QueryExpression("cds_carsclass");
        QueryExpression queryCars = new QueryExpression("cds_car");
        QueryExpression queryAccount = new QueryExpression("account");
        QueryExpression queryUser = new QueryExpression("contact");

        enum Locations
        {
            Airport = 754300000,
            City_center = 754300001,
            Office = 754300002

        }

        List<StatusProbabilities> statusesList = new List<StatusProbabilities>()
        {

            new StatusProbabilities(754300000,"Created", 0.05 ),
            new StatusProbabilities(754300001,"Confirmed", 0.05 ),
            new StatusProbabilities(754300002,"Renting", 0.05 ),
            new StatusProbabilities(754300003,"Returned", 0.75 ),
            new StatusProbabilities(754300004,"Canceled", 0.1 )
        };
        Random random = new Random();

        public AddRecords() 
        { 
            if (!LoadData()) Error = "Can't connect to Dynamics 365";
        }
        public bool LoadData()
        {
            using (var service = new CrmServiceClient(CONNEСTION_STRING))
            {
                if (!service.IsReady)
                {
                    return false;
                }
                queryClasses.ColumnSet = new ColumnSet("cds_classcode");
                entityOfClasses = service.RetrieveMultiple(queryClasses);

                queryCars.ColumnSet = new ColumnSet("cds_vinnumber", "cds_carclass");
                entityOfCars = service.RetrieveMultiple(queryCars);

                entityOfCustomers = service.RetrieveMultiple(queryAccount);

                EntityCollection entityOfUsers = service.RetrieveMultiple(queryUser);
                foreach (var customer in entityOfUsers.Entities)
                { 
                    entityOfCustomers.Entities.Add(customer);
                }
                return true;
            }
        }
        public void AddRent(int NumOfRecords)
        {
            NumOfAddedRecords = 0;
            NumOfAddedReports = 0;
            if (Error != string.Empty) return;

            var rents = new EntityCollection();
            
            double amountOfPages = Math.Ceiling((double)NumOfRecords / 500.0);
            for (int n = 0; n < amountOfPages; n++)
            {
                
                int minValueOfRecords = Math.Min(500, Math.Abs(NumOfRecords-(int)n*500));
                while (minValueOfRecords > 0)
                {
                    rents.Entities.Add(GenerateRent());
                    minValueOfRecords--;
                }
                NumOfAddedRecords += SendRequest(rents);
                NumOfAddedReports += SendRequest(entityOfReports);
            }
        }
        public int SendRequest(EntityCollection entities)
        {
            using (var service = new CrmServiceClient(CONNEСTION_STRING))
            {
                ExecuteMultipleResponse responseWithRents = null;
                ExecuteMultipleRequest requestWithRents = null;

                if (!service.IsReady)
                {
                    Status = false;
                    Error = "Failed to connect to Dynamics 365.";
                    return -1;
                }

                requestWithRents = new ExecuteMultipleRequest()
                {
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = true
                    },
                    Requests = new OrganizationRequestCollection()
                };

                foreach (var entity in entities.Entities)
                {
                    CreateRequest createRequest = new CreateRequest { Target = entity };
                    requestWithRents.Requests.Add(createRequest);
                }
                responseWithRents = (ExecuteMultipleResponse)service.Execute(requestWithRents);

                if (responseWithRents.IsFaulted == false)
                {
                    Status = true;
                    entities.Entities.Clear();
                    return responseWithRents.Responses.Count();
                }

                foreach (var responseItem in responseWithRents.Responses)
                {
                    if (responseItem.Fault != null)
                    {
                        Status = false;
                        Error = (requestWithRents.Requests[responseItem.RequestIndex],
                            responseItem.RequestIndex, responseItem.Fault).ToString();
                        return -1;
                    }
                }
                return 0;
            }
        }

        private Entity GenerateRent()
        {
            var rental = new Entity("cds_rent");
            rental["cds_reservedpickup"] = GenerateRandomDateTime(new DateTime(2019, 1, 1), new DateTime(2020, 12, 31));
            rental["cds_reservedhandover"] = GenerateRandomDateTime(
                rental.GetAttributeValue<DateTime>("cds_reservedpickup"), 
                rental.GetAttributeValue<DateTime>("cds_reservedpickup").AddDays(random.Next(1, 30)));
            rental["cds_carclass"] = GetRandomCarClass();
            rental["cds_car"] = GetRandomCar(rental.GetAttributeValue<EntityReference>("cds_carclass"));
            rental["cds_customer"] = GetRandomCustomer();
            rental["cds_pickuplocation"] = GetRandomLocation();
            rental["cds_returnlocation"] = GetRandomLocation();
            rental["cds_actualpickup"] = rental.GetAttributeValue<DateTime>("cds_reservedhandover")
                .AddDays(random.Next(0, 5)); ;
            rental["statuscode"] = GetRandomStatus(statusesList,rental);
            rental["cds_paid"] = GetRandomPaidStatus(statusesList, 
                rental.GetAttributeValue<OptionSetValue>("statuscode"));
            //cds_actualreturn fill in GetRandomStatus
            return rental;
        }

        private DateTime GenerateRandomDateTime(DateTime start, DateTime end)
        {
            var range = end - start;
            var randomTimeSpan = new TimeSpan((long)(random.NextDouble() * range.Ticks));
            return start + randomTimeSpan;
        }

        private EntityReference GetRandomCarClass() => GetRandomReference(entityOfClasses);

        private EntityReference GetRandomCar(EntityReference carClass)
        {
            
            List<Entity> listOfCars = entityOfCars.Entities.Where(e => e.GetAttributeValue
            <EntityReference>("cds_carclass").Id == carClass.Id).ToList();
            EntityCollection filteredCars = new EntityCollection(listOfCars);
            return GetRandomReference(filteredCars);
        }

        private EntityReference GetRandomCustomer() => GetRandomReference(entityOfCustomers);
        private OptionSetValue GetRandomLocation()
        {
            int firstLocation = (int)Enum.GetValues(typeof(Locations)).GetValue(0);
            int lastLocation = (int)Enum.GetValues(typeof(Locations)).GetValue
                (Enum.GetValues(typeof(Locations)).Length - 1);
            int randomIndex = random.Next(firstLocation, lastLocation);
            if (randomIndex <= lastLocation && randomIndex >= firstLocation)
            {
                var selectedValue = randomIndex;
                return new OptionSetValue(selectedValue);
            }
            else
            {
                return new OptionSetValue(firstLocation);
            }
        }

        public OptionSetValue GetRandomStatus(List<StatusProbabilities> statuses,Entity rental)
        {
            double rand = random.NextDouble();

            double cumulative = 0.0;
            for (int i = 0; i < statuses.Count; i++)
            {
                cumulative += statuses[i].Probability;
                if (rand < cumulative)
                {
                    if(statuses[i].Status == "Renting")
                    {
                        CreateCarReport(rental.GetAttributeValue<EntityReference>("cds_car")
                            , false/*pickup*/, rental.GetAttributeValue<DateTime>("cds_actualpickup"));
                    }
                    else if (statuses[i].Status == "Returned")
                    {
                        rental["cds_actualreturn"] = rental.GetAttributeValue<DateTime>("cds_actualpickup")
                           .AddDays(random.Next(3, 40));

                        CreateCarReport(rental.GetAttributeValue<EntityReference>("cds_car")
                            , false/*pickup*/, rental.GetAttributeValue<DateTime>("cds_actualpickup"));

                        CreateCarReport(rental.GetAttributeValue<EntityReference>("cds_car")
                            , true/*return*/, rental.GetAttributeValue<DateTime>("cds_actualpickup")
                            , rental.GetAttributeValue<DateTime>("cds_actualreturn"));
                       
                    }
                    return new OptionSetValue(statuses[i].Code);
                }
            }
            return new OptionSetValue(statuses.First().Code);
        }
        public void CreateCarReport(EntityReference car,bool type, DateTime actualpickup, DateTime actualreturn = new DateTime())
        {/*Type: false - pickup, true - return*/
            Entity carReport = new Entity("cds_cartransferreport");

            bool damageStatus = GetRandomDamageStatus();

            carReport["cds_car"] = car;
            carReport["cds_type"] = type;
            carReport["cds_damages"] = damageStatus;
            carReport["cds_damagedescription"] = null;

            if (damageStatus)
            { 
                carReport["cds_damagedescription"] = "Some damages";
            }
            if (type)
            { 
                carReport["cds_date"] = actualreturn;
            }
            else
            {
                carReport["cds_date"] = actualpickup;
            }
            entityOfReports.Entities.Add(carReport);
        }
        public bool GetRandomDamageStatus()
        {
            double yes = 0.05;

            double randomValue = random.NextDouble();

            if (randomValue < yes)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool GetRandomPaidStatus(List<StatusProbabilities> statuses,OptionSetValue code)
        {
            string status = statuses.Where(e => e.Code == code.Value).Select(a => a.Status).First();
            double prob=0.0;
            switch(status)
            {
                case "Confirmed":
                {
                    prob = 0.9; 
                    break;
                }
                case "Renting":
                {
                    prob = 0.999;
                    break;
                }
                case "Returned":
                {
                    prob = 0.9998;
                    break;
                }
            }
            double yes = prob;
            double no = 1-prob;

            double randomValue = random.NextDouble();

            if (randomValue < yes)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private EntityReference GetRandomReference(EntityCollection collection)
        {
            int randomIndex = random.Next(collection.Entities.Count);

            foreach (var str in collection.Entities)
            {
                if (randomIndex == 0)
                {
                    return str.ToEntityReference();
                }

                randomIndex--;
            }
            throw new InvalidOperationException("Collection is empty or random index is incorrect.");
        }


        public int GetNumOfAddedRecords() => NumOfAddedRecords;
        public int GetNumOfAddedReports() => NumOfAddedReports;


        public bool GetStatus() => Status;
        public string GetError() => Error;

    }
}

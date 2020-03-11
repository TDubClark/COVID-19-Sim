using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorApp1.Data
{
    public class State
    {
        public DateTime SimStart { get; set; }
        public DateTime CurrentDateTime { get; set; }
        public double HoursFromInfectionToContegeousness { get; set; }
        public int InfectionDurationDays { get; set; }
        /// <summary>
        /// After this number of days, the infection becomes either severe or not.
        /// </summary>
        public int InfectionIncubationDurationDays { get; set; }
        public float RValue { get; set; }
        /// <summary>
        /// Severe - requires intesive treatment or will die
        /// </summary>
        public float SeverityRate { get; set; }
        /// <summary>
        /// Fatality rate of severe cases which receive intensive treatment
        /// </summary>
        public float SeverityRateFatal { get; set; }
        /// <summary>
        /// The mortality rate for mild cases
        /// </summary>
        public float MortalityRateMildCases { get; set; }
        public float VaccinationRate { get; set; }

        /// <summary>
        /// The rate at which known cases are quarantined.
        /// </summary>
        public float QuarantineRate { get; set; }

        public int HospitalIntensiveTreatementCapacity { get; set; }


        /// <summary>
        /// Running total on hospital capacity
        /// </summary>
        public float RunningHospitalCapacity { get; set; }
        public int RunningDeathsTotal { get; set; }
        public float RunningResolvedDeathRate { get; set; }


        public float GetInfectionLikelihoodPerDay() => RValue / (float)InfectionDurationDays;

        public void Restart()
        {
            this.CurrentDateTime = SimStart;
        }

        public bool IsContageous(Person person)
        {
            if (person.IsInfected())
                return CurrentDateTime.Subtract(person.DateInfected.Value).TotalHours >= HoursFromInfectionToContegeousness;
            return false;
        }

        /// <summary>
        /// Runs the model.
        /// </summary>
        /// <param name="people"></param>
        /// <param name="increment"></param>
        public void RunModel(Person[] people, TimeSpan increment)
        {
            CurrentDateTime = CurrentDateTime.Add(increment);
            var random = new Random();
            foreach (var person in getInfected(people))
            {
                if (person.IsInfected())
                {
                    if (!DetermineConclusion(random, person))
                    {
                        DetermineSeverity(random, person);
                        RiskTransmission(random, person, people);
                    }
                }
            }
            Triage(getInfectedSevere(people));

            RunningHospitalCapacity = getHospitalCapacity(people);
            RunningDeathsTotal = countDeaths(people);
            RunningResolvedDeathRate = getResolvedDeathRate(people);
        }

        static int countDeaths(Person[] people)
        {
            return people.Count(x => !x.Alive);
        }

        static float getResolvedDeathRate(Person[] people)
        {
            return (float)people.Count(x => !x.Alive) / (float)people.Count(x => x.IsResolved());
        }

        public float getHospitalCapacity(Person[] people)
        {
            return (float)getInfectedSevere(people).Length / (float)this.HospitalIntensiveTreatementCapacity;
        }

        /// <summary>
        /// Perform triage on severe infected patients - order by age, and leave the oldest to take their chances.
        /// </summary>
        /// <param name="person"></param>
        void Triage(Person[] person)
        {
            if (this.HospitalIntensiveTreatementCapacity < person.Length)
            {
                var deprioritized = person.OrderByDescending(x => x.Age).Take(person.Length - this.HospitalIntensiveTreatementCapacity).ToList();
                deprioritized.ForEach(x => x.Deceased());
            }
        }

        Person[] getInfected(Person[] people)
        {
            return people.Where(x => x.IsInfected()).ToArray();
        }
        Person[] getInfectedSevere(Person[] people)
        {
            return people.Where(x => x.IsInfected() && x.Severity == InfectionSeverity.Severe).ToArray();
        }

        /// <summary>
        /// Determines a conclusion to the person; returns whether concluded.
        /// </summary>
        /// <param name="random"></param>
        /// <param name="person"></param>
        /// <returns></returns>
        bool DetermineConclusion(Random random, Person person)
        {
            if (this.CurrentDateTime.Subtract(person.DateInfected.Value).TotalDays >= this.InfectionDurationDays)
            {
                switch (person.Severity)
                {
                    case InfectionSeverity.NotApplicable:
                        break;
                    case InfectionSeverity.Incubating:
                        break;
                    case InfectionSeverity.Severe:
                        if (random.NextDouble() <= SeverityRateFatal)
                            person.Deceased(); // This person died (a random decimal was <= the mortality rate)
                        else
                            person.Recover(this.CurrentDateTime);
                        break;
                    case InfectionSeverity.Mild:
                        if (random.NextDouble() <= MortalityRateMildCases)
                            person.Deceased(); // This person died (a random decimal was <= the mortality rate)
                        else
                            person.Recover(this.CurrentDateTime);
                        break;
                    default:
                        break;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines a conclusion to the person; returns whether concluded.
        /// </summary>
        /// <param name="random"></param>
        /// <param name="person"></param>
        /// <returns></returns>
        bool DetermineSeverity(Random random, Person person)
        {
            if (person.Severity == InfectionSeverity.Incubating && this.CurrentDateTime.Subtract(person.DateInfected.Value).TotalDays >= this.InfectionIncubationDurationDays)
            {
                if (random.NextDouble() < SeverityRate)
                    person.AssignSeverity(InfectionSeverity.Severe); // This person received a severe case (a random decimal was < the severity rate)
                else
                    person.AssignSeverity(InfectionSeverity.Mild);
                person.IsQuarantined = random.NextDouble() < QuarantineRate;  // This person quarantined themselves (a random decimal was < the quarantine rate)
                return true;
            }
            return false;
        }

        /// <summary>
        /// This calculates the risk of someone contracting an infection.
        /// </summary>
        /// <param name="random"></param>
        /// <param name="person"></param>
        /// <param name="people"></param>
        void RiskInfection(Random random, Person person, Person[] people)
        {
            bool isinfected = new Random().NextDouble() * getInfectionRisk(person) * getTransmissionRisk(people) >= 0.5;
            if (isinfected)
                person.Infect(CurrentDateTime);
        }

        /// <summary>
        /// This calculates the risk of an infected person transmitting the virus to uninfected.
        /// </summary>
        /// <param name="random"></param>
        /// <param name="person"></param>
        /// <param name="people"></param>
        void RiskTransmission(Random random, Person person, Person[] people)
        {
            bool isinfected;
            int i = 0;
            var healthy = getHealthy(people).ToArray();
            foreach (var p in getHealthy(people))
            {
                if (p.ID == person.ID || p.IsInfected() || !p.Alive)
                    continue;

                // Skip quarantined people
                if (p.IsQuarantined)
                    continue;

                isinfected = random.NextDouble() <= (GetInfectionLikelihoodPerDay() * 2 / (float)healthy.Length);
                if (isinfected && !p.Vaccinated)
                    p.Infect(CurrentDateTime);
                i++;
                // Only risk to 10 people per day.
                //if (i >= 10)
                //    break;
            }
        }
        IEnumerable<Person> getHealthy(IEnumerable<Person> people) => people.Where(x => x.IsHealthy());

        // I just pulled these formulas out of thin air:
        double getInfectionRisk(Person person)
        {
            //throw new NotImplementedException();
            return (person.ContactHygeine * .2 + person.RespirtoryHygeine * .8)
                * ((double)person.Age / 80.0) * person.Health;
        }
        double getTransmissionRisk(Person[] person)
        {
            return person.Sum(x => getTransmissionRisk(x)) * 2 / (double)person.Count(x => x.IsInfected());
        }
        double getTransmissionRisk(Person person)
        {
            // How to implement this?
            //throw new NotImplementedException();
            return (person.IsInfected() ? 1 : 0) * (person.ContactHygeine * .2 + person.RespirtoryHygeine * .8);
        }
    }
}

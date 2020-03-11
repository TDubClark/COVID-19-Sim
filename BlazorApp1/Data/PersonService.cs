using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorApp1.Data
{
    public class PersonService
    {
        public Task<Person[]> GetPeopleAsync(State state, int population)
        {
            var rng = new Random();
            int id = 0;
            return Task.FromResult(Enumerable.Range(1, population).Select(index => new Person(id++, rng.Next(0, 95))
            {
                Health = (float)rng.NextDouble(),
                ContactHygeine = (float)rng.NextDouble(),
                RespirtoryHygeine = (float)rng.NextDouble(),
                Vaccinated = (float)rng.NextDouble() < state.VaccinationRate
            }).ToArray().InfectOne(state));
        }
    }
    public static class PersonExt
    {
        /// <summary>
        /// Infects one person
        /// </summary>
        /// <param name="people"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static Person[] InfectOne(this Person[] people, State state)
        {
            people[new Random().Next(0, people.Length - 1)].Infect(state.SimStart);
            return people;
        }
    }
}

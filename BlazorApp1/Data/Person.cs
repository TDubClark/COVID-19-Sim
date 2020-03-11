using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorApp1.Data
{
    public enum InfectionSeverity
    {
        NotApplicable,
        Incubating,
        Severe,
        Mild,
        Resolved
    }

    public class Person : IFormattable
    {
        public int ID { get; set; }
        public int Age { get; set; }
        public float Health { get; set; }
        public float ContactHygeine { get; set; }
        public float RespirtoryHygeine { get; set; }

        public DateTime? DateInfected { get; private set; }
        public DateTime? DateRecovered { get; private set; }
        public bool Alive { get; private set; } = true;
        public bool Vaccinated { get; set; } = false;

        public InfectionSeverity Severity { get; private set; } = InfectionSeverity.NotApplicable;

        public bool IsQuarantined { get; set; } = false;

        public void AssignSeverity(InfectionSeverity severity)
        {
            this.Severity = severity;
        }

        /// <summary>
        /// Infects this person at the given time; person starts their incubation period.
        /// </summary>
        /// <param name="infectionTime"></param>
        public void Infect(DateTime infectionTime)
        {
            this.Severity = InfectionSeverity.Incubating;
            this.DateInfected = infectionTime;
        }

        public void Recover(DateTime recoverTime)
        {
            this.Severity = InfectionSeverity.Resolved;
            this.DateRecovered = recoverTime;
        }

        /// <summary>
        /// Whether this person's case is resolved (either by recovery or death)
        /// </summary>
        public bool IsResolved()
        {
            return !this.Alive || this.DateRecovered.HasValue;
        }

        public void Deceased()
        {
            this.Alive = false;
        }

        public bool IsInfected()
        {
            return DateInfected.HasValue && !DateRecovered.HasValue && Alive;
        }

        public bool IsHealthy() => !DateInfected.HasValue;

        public float GetInfectionLikelihood(State state)
        {
            return 0.5f;
        }

        public Person(int id, int age)
        {
            this.ID = id;
            this.Age = age;
            this.DateInfected = null;
            this.DateRecovered = null;
            this.Alive = true;
        }

        public string getCode()
        {
            if (!Alive) return "d";
            if (Vaccinated) return "v";
            if (IsInfected()) return getInfectedCode();
            if (DateRecovered.HasValue) return "r";
            return "h";
        }

        string getInfectedCode()
        {
            switch (Severity)
            {
                case InfectionSeverity.NotApplicable:
                    break;
                case InfectionSeverity.Incubating:
                    if (IsQuarantined)
                        return "qi";
                    return "i";
                case InfectionSeverity.Severe:
                    if (IsQuarantined)
                        return "qs";
                    return "s";
                case InfectionSeverity.Mild:
                    if (IsQuarantined)
                        return "qm";
                    return "m";
            }
            if (IsQuarantined)
                return "qi";
            return "i";
        }

        public override string ToString()
        {
            return ToString("s", System.Globalization.CultureInfo.CurrentCulture);
        }

        public string ToString(string format)
        {
            return ToString(format, System.Globalization.CultureInfo.CurrentCulture);
        }
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null) format = "s";
            switch (format.ToLower())
            {
                case "c":
                    return getCode();
                case "age":
                    return this.Age.ToString();
                default:
                    var code = getCode();
                    if (code.Equals("h"))
                        return this.Age.ToString();
                    return String.Format("{0:d}/{1}", this.Age, code);
                    //if (this.Alive)
                    //{
                    //    if (DateRecovered.HasValue)
                    //        return String.Format("{0:d}/Rec", this.Age);
                    //    if (DateInfected.HasValue)
                    //        return String.Format("{0:d}/Inf", this.Age);
                    //    return this.Age.ToString();
                    //}
                    //return String.Format("{0:d}/Died", this.Age);
            }
        }
    }
}

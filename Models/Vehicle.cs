using System;

namespace PragueParking.Data.Models
{
    public abstract class Vehicle
    {
        public string Registration { get; set; }
        public DateTime EntryTime { get; set; }

        // Initialize to empty string to avoid nullability warning.
        public string Type { get; protected set; } = string.Empty;

        protected Vehicle(string registration)
        {
            if (string.IsNullOrWhiteSpace(registration))
                throw new ArgumentException("Registration must be provided", nameof(registration));

            Registration = registration;
            EntryTime = DateTime.Now;
        }

        // Storleksfaktor: 1.0 = standard bilplats. MC = 0.5.
        public abstract double Size { get; }
    }
}

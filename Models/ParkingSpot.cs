using System.Collections.Generic;
using System.Linq;

namespace PragueParking.Data.Models
{
    public class ParkingSpot
    {
        public int Id { get; set; }
        public double Capacity { get; set; } = 1.0;
        public List<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public bool IsEmpty => Vehicles.Count == 0;
        public bool IsFull => Vehicles.Sum(v => v.Size) >= Capacity - 1e-9;

        public bool TryAddVehicle(Vehicle vehicle)
        {
            if (vehicle == null) return false;
            double used = Vehicles.Sum(v => v.Size);
            if (used + vehicle.Size <= Capacity + 1e-9)
            {
                Vehicles.Add(vehicle);
                return true;
            }
            return false;
        }

        public bool RemoveVehicle(string registration)
        {
            var v = Vehicles.Find(x => x.Registration.Equals(registration, System.StringComparison.OrdinalIgnoreCase));
            if (v == null) return false;
            Vehicles.Remove(v);
            return true;
        }

        public Vehicle? FindVehicle(string registration)
        {
            return Vehicles.Find(x => x.Registration.Equals(registration, System.StringComparison.OrdinalIgnoreCase));
        }

        public override string ToString()
        {
            if (IsEmpty) return "Tom";
            return string.Join(", ", Vehicles.Select(v => $"{v.Type}:{v.Registration}"));
        }
    }
}

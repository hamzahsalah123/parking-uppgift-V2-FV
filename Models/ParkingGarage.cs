using System;
using System.Collections.Generic;
using System.Linq;

namespace PragueParking.Data.Models
{
    public class ParkingGarage
    {
        public List<ParkingSpot> Spots { get; set; } = new List<ParkingSpot>();

        public ParkingGarage() : this(100) { }

        public ParkingGarage(int numberOfSpots)
        {
            if (numberOfSpots <= 0) numberOfSpots = 100;
            for (int i = 1; i <= numberOfSpots; i++)
                Spots.Add(new ParkingSpot { Id = i, Capacity = 1.0 });
        }

        public ParkingSpot? FindSpotByRegistration(string registration)
        {
            return Spots.FirstOrDefault(s => s.Vehicles.Any(v => v.Registration.Equals(registration, StringComparison.OrdinalIgnoreCase)));
        }

        public (bool success, int spotId) TryParkVehicle(Vehicle v)
        {
            foreach (var spot in Spots)
            {
                if (spot.TryAddVehicle(v))
                    return (true, spot.Id);
            }
            return (false, -1);
        }

        public bool RemoveVehicle(string registration, out Vehicle? removed, out int spotId)
        {
            removed = null;
            spotId = -1;
            foreach (var spot in Spots)
            {
                var veh = spot.FindVehicle(registration);
                if (veh != null)
                {
                    removed = veh;
                    spotId = spot.Id;
                    return spot.RemoveVehicle(registration);
                }
            }
            return false;
        }

        public (ParkingSpot? spot, Vehicle? vehicle) GetVehicle(string registration)
        {
            foreach (var spot in Spots)
            {
                var veh = spot.FindVehicle(registration);
                if (veh != null) return (spot, veh);
            }
            return (null, null);
        }
    }
}

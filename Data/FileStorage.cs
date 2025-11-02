using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PragueParking.Data.Models;

namespace PragueParking.Data
{
    // File-based storage for garage state, prices and config.
    public class FileStorage
    {
        public string GarageFile { get; init; } = "garage.json";
        public string ConfigFile { get; init; } = "config.json";
        public string PricesFile { get; init; } = "prices.txt";

        // DTOs for serialization
        private record VehicleDto(string Type, string Registration, DateTime EntryTime);
        private record SpotDto(int Id, double Capacity, List<VehicleDto> Vehicles);
        private record GarageDto(List<SpotDto> Spots);

        public void SaveGarage(ParkingGarage garage)
        {
            var dto = new GarageDto(garage.Spots.Select(s =>
                new SpotDto(s.Id, s.Capacity,
                    s.Vehicles.Select(v => new VehicleDto(v.Type, v.Registration, v.EntryTime)).ToList()
                )
            ).ToList());

            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GarageFile, json);
        }

        public ParkingGarage LoadGarage(int defaultSpotCount = 100)
        {
            if (!File.Exists(GarageFile))
                return new ParkingGarage(defaultSpotCount);

            try
            {
                var json = File.ReadAllText(GarageFile);
                var dto = JsonSerializer.Deserialize<GarageDto>(json);
                if (dto == null) return new ParkingGarage(defaultSpotCount);

                var garage = new ParkingGarage(0);
                foreach (var s in dto.Spots.OrderBy(x => x.Id))
                {
                    var spot = new ParkingSpot { Id = s.Id, Capacity = s.Capacity };
                    foreach (var v in s.Vehicles)
                    {
                        Vehicle? veh = v.Type switch
                        {
                            "Car" => new Car(v.Registration),
                            "MC" => new Motorcycle(v.Registration),
                            _ => null
                        };
                        if (veh != null)
                        {
                            veh.EntryTime = v.EntryTime;
                            spot.Vehicles.Add(veh);
                        }
                    }
                    garage.Spots.Add(spot);
                }
                return garage;
            }
            catch
            {
                return new ParkingGarage(defaultSpotCount);
            }
        }

        // Prices: simple format KEY=VALUE with optional comments starting with '#'
        public Dictionary<string, double> LoadPrices()
        {
            var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["Car"] = 20.0,
                ["MC"] = 10.0
            };

            if (!File.Exists(PricesFile)) return dict;

            var lines = File.ReadAllLines(PricesFile);
            foreach (var raw in lines)
            {
                var line = raw;
                int idx = line.IndexOf('#');
                if (idx >= 0) line = line.Substring(0, idx);
                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;
                var key = parts[0].Trim();
                if (double.TryParse(parts[1].Trim(), out double val))
                    dict[key] = val;
            }
            return dict;
        }

        // config.json example: { "NumberOfSpots": 100 }
        public int LoadConfigSpotCount()
        {
            if (!File.Exists(ConfigFile)) return 100;
            try
            {
                var json = File.ReadAllText(ConfigFile);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("NumberOfSpots", out var el) && el.ValueKind == JsonValueKind.Number)
                {
                    if (el.TryGetInt32(out int n) && n > 0) return n;
                }
            }
            catch { /* ignore */ }
            return 100;
        }
    }
}

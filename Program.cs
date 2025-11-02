using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using PragueParking.Data;
using PragueParking.Data.Models;

namespace PragueParking.App
{
    internal class Program
    {
        static FileStorage storage = new();
        static ParkingGarage garage = null!;
        static Dictionary<string, double> prices = null!;
        const int FreeMinutes = 10;

        static void Main()
        {
            Console.Title = "Fivestar Parking – 00P ";

            int spots = storage.LoadConfigSpotCount();
            garage = storage.LoadGarage(spots);
            prices = storage.LoadPrices();

            AnsiConsole.MarkupLine("[bold green]Prague Parking 2.0[/]");
            MainMenu();
        }

        static void MainMenu()
        {
            while (true)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Välj ett alternativ:")
                        .AddChoices(new[] {
                            "1. Parkera fordon",
                            "2. Hämta fordon",
                            "3. Flytta fordon",
                            "4. Sök fordon",
                            "5. Visa karta (översikt)",
                            "6. Läs in prisfil på nytt",
                            "0. Avsluta och spara"
                        })
                );

                switch (choice[0])
                {
                    case '1': ParkVehicle(); break;
                    case '2': RetrieveVehicle(); break;
                    case '3': MoveVehicle(); break;
                    case '4': SearchVehicle(); break;
                    case '5': ShowOverview(); break;
                    case '6': ReloadPrices(); break;
                    case '0': Exit(); return;
                    default: AnsiConsole.MarkupLine("[red]Ogiltigt val[/]"); break;
                }
            }
        }

        static void ParkVehicle()
        {
            var type = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Välj fordonstyp:")
                .AddChoices("Car", "MC"));

            var reg = AnsiConsole.Ask<string>("Registreringsnummer (max 10):").Trim().ToUpper();
            if (string.IsNullOrEmpty(reg) || reg.Length > 10)
            {
                AnsiConsole.MarkupLine("[red]Ogiltigt registreringsnummer[/]");
                return;
            }

            Vehicle? v = type switch
            {
                "Car" => new Car(reg),
                "MC" => new Motorcycle(reg),
                _ => null
            };

            if (v == null)
            {
                AnsiConsole.MarkupLine("[red]Okänd typ[/]");
                return;
            }

            var (success, spotId) = garage.TryParkVehicle(v);
            if (success)
            {
                storage.SaveGarage(garage);
                AnsiConsole.MarkupLine($"[green]Parkerat i ruta {spotId}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Ingen plats funnen[/]");
            }
        }

        static void RetrieveVehicle()
        {
            var reg = AnsiConsole.Ask<string>("Ange registreringsnummer:").Trim().ToUpper();
            if (string.IsNullOrEmpty(reg)) return;

            if (garage.RemoveVehicle(reg, out Vehicle? removed, out int spotId))
            {
                var duration = DateTime.Now - (removed?.EntryTime ?? DateTime.Now);
                double fee = CalculateFee(removed!, duration);
                storage.SaveGarage(garage);
                AnsiConsole.MarkupLine($"[green]Hämtat {removed!.Type} {removed.Registration} från ruta {spotId}[/]");
                AnsiConsole.MarkupLine($"Tid: {FormatDuration(duration)}");
                AnsiConsole.MarkupLine($"Avgift: {fee} CZK");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Fann inget fordon med det registret[/]");
            }
        }

        static double CalculateFee(Vehicle v, TimeSpan duration)
        {
            if (duration.TotalMinutes <= FreeMinutes) return 0.0;
            double perHour = prices.ContainsKey(v.Type) ? prices[v.Type] : (v.Type == "Car" ? 20.0 : 10.0);
            double hours = Math.Ceiling(duration.TotalMinutes / 60.0);
            return perHour * hours;
        }

        static string FormatDuration(TimeSpan t)
        {
            return $"{(int)t.TotalHours}h {t.Minutes}m";
        }

        static void MoveVehicle()
        {
            var reg = AnsiConsole.Ask<string>("Ange registreringsnummer att flytta:").Trim().ToUpper();
            var (spot, vehicle) = garage.GetVehicle(reg);
            if (vehicle == null)
            {
                AnsiConsole.MarkupLine("[red]Fordonet hittades inte[/]");
                return;
            }

            int to = AnsiConsole.Ask<int>("Ange målruta (1-based):");
            if (to < 1 || to > garage.Spots.Count)
            {
                AnsiConsole.MarkupLine("[red]Ogiltigt rutanummer[/]");
                return;
            }

            spot!.RemoveVehicle(reg);
            bool ok = garage.Spots[to - 1].TryAddVehicle(vehicle);
            if (!ok)
            {
                // rollback
                spot.TryAddVehicle(vehicle);
                AnsiConsole.MarkupLine("[red]Målruta kan inte ta fordonet[/]");
            }
            else
            {
                storage.SaveGarage(garage);
                AnsiConsole.MarkupLine("[green]Flyttad[/]");
            }
        }

        static void SearchVehicle()
        {
            var reg = AnsiConsole.Ask<string>("Ange registreringsnummer:").Trim().ToUpper();
            var (spot, vehicle) = garage.GetVehicle(reg);
            if (vehicle == null)
                AnsiConsole.MarkupLine("[yellow]Inte hittad[/]");
            else
                AnsiConsole.MarkupLine($"[green]Hittad i ruta {spot!.Id}: {vehicle.Type} {vehicle.Registration}[/]");
        }

        static void ShowOverview()
        {
            int free = garage.Spots.Count(s => s.IsEmpty);
            int total = garage.Spots.Count;
            AnsiConsole.MarkupLine($"Lediga platser: [green]{free}[/]/[yellow]{total}[/]");

            var grid = new Grid().AddColumn().AddColumn().AddColumn().AddColumn();

            for (int i = 0; i < garage.Spots.Count; i += 4)
            {
                var cells = new List<string>();
                for (int c = 0; c < 4; c++)
                {
                    int idx = i + c;
                    if (idx >= garage.Spots.Count)
                    {
                        cells.Add("");
                        continue;
                    }

                    var spot = garage.Spots[idx];
                    string spotNumber = Markup.Escape((idx + 1).ToString());
                    string content;

                    if (spot.IsEmpty)
                        content = $"[green][[{spotNumber}]] - Tom[/]";
                    else
                        content = $"[red][[{spotNumber}]] - {spot.Vehicles.Count} st[/]";

                    cells.Add(content);
                }
                grid.AddRow(cells.ToArray());
            }

            AnsiConsole.Write(grid);
            Console.ReadKey(true);
        }

        static void ReloadPrices()
        {
            prices = storage.LoadPrices();
            AnsiConsole.MarkupLine("[green]Prisfil inläst[/]");
        }

        static void Exit()
        {
            storage.SaveGarage(garage);
            AnsiConsole.MarkupLine("[yellow]Sparat. Hejdå![/]");
        }
    }
}

namespace PragueParking.Data.Models
{
    public class Motorcycle : Vehicle
    {
        public Motorcycle(string registration) : base(registration)
        {
            Type = "MC";
        }

        public override double Size => 0.5;
    }
}

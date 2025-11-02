namespace PragueParking.Data.Models
{
    public class Car : Vehicle
    {
        public Car(string registration) : base(registration)
        {
            Type = "Car";
        }

        public override double Size => 1.0;
    }
}

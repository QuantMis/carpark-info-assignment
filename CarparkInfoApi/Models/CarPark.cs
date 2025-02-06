namespace CarparkInfoApi.Models;
public class CarPark
{
    public int Id { get; set; }
    public string CarParkNo { get; set; }
    public string Address { get; set; }
    public double XCoord { get; set; }
    public double YCoord { get; set; }
    public int CarParkTypeId { get; set; }
    public int ParkingSystemId { get; set; }
    public int ShortTermParkingId { get; set; }
    public bool FreeParking { get; set; }
    public bool NightParking { get; set; }
    public int CarParkDecks { get; set; }
    public float GantryHeight { get; set; }
    public bool CarParkBasement { get; set; }

    public CarParkType CarParkType { get; set; }
    public ParkingSystem ParkingSystem { get; set; }
    public ShortTermParking ShortTermParking { get; set; }
}


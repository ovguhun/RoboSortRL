namespace RoboSortRL.Simulation
{
    public enum ProductType
    {
        Good = 0,
        Defective = 1
    }

    public enum ZoneType
    {
        Accept = 0,
        Reject = 1
    }

    public enum SortingOutcome
    {
        GoodAccepted = 0,
        DefectRejected = 1,
        GoodRejected = 2,
        DefectMissed = 3
    }
}
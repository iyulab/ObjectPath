namespace ObjectPathLibrary.Tests
{
    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public Address Address { get; set; } = new();
    }

    public class Address
    {
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
    }

    public record Employee(string Name, int Id, Department Department);

    public record Department(string Name, Manager Manager);

    public record Manager(string Name, string Email);
}

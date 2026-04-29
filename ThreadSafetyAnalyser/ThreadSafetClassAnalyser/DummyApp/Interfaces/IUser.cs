namespace DommyApp.Interfaces;

public interface IUser
{
    // Property get and set methods
    public string FirstName { get; set; }
    // Normal get / set methods
    public string GetLastName();
    public void SetLastName(string lastName);
    
    
}
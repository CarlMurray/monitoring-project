using System;

namespace Models.Actions;

public interface IAction
{
    public Guid Id { get; init; }
    public string? Name { get; }
    public string? Description { get; set; }

    public void Trigger();
}

public class DevAction : IAction
{
    public Guid Id { get; init; }
    public string? Name { get; }
    public string? Description { get; set; }

    public DevAction(string id)
    {
        Id = Guid.Parse(id);
        Name = "";
        Description = "";
    }

    public void Trigger()
    {
        Console.WriteLine("Action triggered.");
    }
}
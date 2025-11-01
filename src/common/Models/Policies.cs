namespace Models.Policies;

using Models.Actions;
using Models.DataPoints;
using ClickHouse.Driver.ADO;
using System.Threading.Tasks;
using System.Text.Json;
using System.ComponentModel;
using System.Text.Json.Nodes;
using System.Collections;

public class Policy
{
    public Guid Id { get; } = Guid.NewGuid();
    public bool State { get; set; } = false;
    public string Name { get; set; } = "";
    public string? Description { get; set; } = "";
    public List<ConditionSet> Conditions { get; set; } = new();
    public List<IAction> Actions { get; set; } = new();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    // TODO
    // public User CreatedBy {get;}
    public Policy()
    {

    }

    public Policy(string name, bool state, List<ConditionSet> conditions, List<IAction> actions)
    {
        Name = name;
        State = state;
        Conditions = conditions;
        Actions = actions;
    }

    public Policy(string name, bool state, string description, List<ConditionSet> conditions, List<IAction> actions) : this(name, state, conditions, actions)
    {
        Description = description;
    }

    public Policy(string id, string name, bool state, string description, List<ConditionSet> conditions, List<IAction> actions) : this(name, state, description, conditions, actions)
    {
        Id = Guid.Parse(id);
    }

    public PolicyEvaluationResult Evaluate(INormalisedDataPoint dataPoint)
    {
        bool isConditionSetMatched = false;
        List<Condition>? conditionsMatched = new();
        foreach (ConditionSet conditionSet in Conditions)
        {
            foreach (Condition condition in conditionSet.Conditions)
            {
                if (condition.Evaluate(dataPoint))
                {
                    // condition.State = true;
                    conditionsMatched.Add(condition);
                }
            }
            // Return a positive result only if all conditions in a condition set are true
            if (conditionSet.Conditions.All(c => c.State == true))
            {
                isConditionSetMatched = true;
                return new PolicyEvaluationResult(isConditionSetMatched, dataPoint, Id.ToString(), conditionsMatched);
            }
        }
        // Else return a negative result
        return new PolicyEvaluationResult(isConditionSetMatched, dataPoint, Id.ToString(), conditionsMatched);
    }
}

public class PolicyEngine
{
    private ClickHouseConnection _db;
    public List<Policy> Policies { get; set; } = new();

    public PolicyEngine(ClickHouseConnection db)
    {
        _db = db;
        _db.ConnectionString = String.IsNullOrEmpty(_db.ConnectionString)
            ? $"Host={Environment.GetEnvironmentVariable("CH_HOST")};Port=8443;Protocol=https;Database=cooked_metrics;Username=default;Password={Environment.GetEnvironmentVariable("CH_PASSWORD")}"
            : _db.ConnectionString;
        _db.Open();
        Task result = FetchPolicies();
    }

    public void ExecutePolicy(Policy policy, INormalisedDataPoint dataPoint)
    {

        if (policy.Evaluate(dataPoint).State)
        {
            foreach (IAction action in policy.Actions)
            {
                action.Trigger();
            }
        }

    }

    public async Task FetchPolicies()
    {
        Policies.Clear();
        _db.Open();
        var command = _db.CreateCommand();
        command.CommandText = "SELECT * FROM cooked_metrics.policies WHERE state = true";
        // command.CommandText = "SELECT * FROM cooked_metrics.policies WHERE enabled == true FORMAT JSONEachRow";
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetString(0);
            var state = reader.GetBoolean(1);
            var name = reader.GetString(2);
            var description = reader.GetString(3);
            JsonArray conditionSets = reader.GetFieldValue<JsonObject>(4)["conditionSets"].AsArray();
            Console.WriteLine(conditionSets.GetType());
            var actions = reader.GetFieldValue<string[]>(5);
            Console.WriteLine();

            var conditionSetsObject = new List<ConditionSet>();

            foreach (var conditionSet in conditionSets)
            {
                Console.WriteLine(conditionSet.GetValue<IEnumerable>());
                Console.WriteLine(conditionSet.GetType());
                Console.WriteLine("done");
                var conditionObjects = new List<Condition>();
                foreach (JsonObject condition in conditionSet.GetValue<IEnumerable>())
                {
                    var timeWindow = Convert.ToInt32(condition["timeWindow"].ToString());
                    var input = Convert.ToDouble(condition["input"].ToString());
                    var threshold = Convert.ToDouble(condition["threshold"].ToString());
                    var operation = condition["operation"].ToString();
                    conditionObjects.Add(new Condition(operation, input, threshold, timeWindow));
                }
                var conditionSetObject = new ConditionSet(conditionObjects);
                conditionSetsObject.Add(conditionSetObject);
            }


            var actionObjects = new List<IAction>();
            foreach (var action in actions)
            {
                actionObjects.Add(new DevAction(action));
            }
            var policy = new Policy(id, name, state, description, conditionSetsObject, actionObjects);
            Policies.Add(policy);
        }
    }
}

public class PolicyEvaluationResult
{
    public Guid Id { get; } = new();
    public bool State { get; set; }
    public INormalisedDataPoint DataPoint { get; }
    public string PolicyId { get; }
    public List<Condition> ConditionsMatched { get; } = new();
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    public PolicyEvaluationResult(bool state, INormalisedDataPoint dataPoint, string policyId, List<Condition> conditionsMatched)
    {
        State = state;
        DataPoint = dataPoint;
        PolicyId = policyId;
        ConditionsMatched = conditionsMatched;
    }
}

public class Condition
{
    // TODO Refactor this to interfaces and classes for each type of condition.
    private string _operation;
    public bool State { get; set; }
    public double Input { get; set; }
    public string Operation
    {
        get => _operation;
        set
        {
            string[] operators = ["=", ">", "<", ">=", "<="];
            if (operators.Contains(value))
            {
                _operation = value;
            }
            else throw new ArgumentOutOfRangeException(message: "Operation must be one of: =, >, <, >=, <=", null);
        }
    }
    public double Threshold { get; set; }
    public int TimeWindow { get; set; } //Seconds

    public Condition()
    {
        _operation = "=";
        State = false;
        Input = 0;
        Threshold = 0;
        TimeWindow = 0;
    }
    public Condition(string operation, double input, double threshold, int timeWindow) : this()
    {
        _operation = operation;
        Input = input;
        Threshold = threshold;
        TimeWindow = timeWindow;
    }

    public bool Evaluate(INormalisedDataPoint dataPoint)
    {
        // TODO
        // Refactor to work with various DataPoint types

        bool result = false;
        switch (_operation)
        {
            case "=":
                result = dataPoint.Value == Threshold;
                break;
            case ">":
                result = dataPoint.Value > Threshold;
                break;
            case ">=":
                result = dataPoint.Value >= Threshold;
                break;
            case "<=":
                result = dataPoint.Value <= Threshold;
                break;
            case "<":
                result = dataPoint.Value < Threshold;
                break;
            default:
                return false;
        }
        State = result;
        return State;

    }
}

public class ConditionSet
{
    public List<Condition> Conditions { get; set; }

    public ConditionSet(List<Condition> conditions)
    {
        Conditions = conditions;
    }
}
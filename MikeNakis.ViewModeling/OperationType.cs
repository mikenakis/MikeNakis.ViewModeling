namespace MikeNakis.ViewModeling;

public enum OperationType
{
	Minor, //does not need a logic entrance
	Medium, //needs a logic entrance but should not cause an application-logic-idle event.
	Major //needs a logic entrance and will cause an application-logic-idle event.
}

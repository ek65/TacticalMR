using Fusion;

public class Player : NetworkBehaviour
{
	private NetworkCharacterControllerPrototype _cc;

	private void Awake()
	{
		_cc = GetComponent<NetworkCharacterControllerPrototype>();
	}

	public override void FixedUpdateNetwork()
	{
		if (GetInput(out NetworkInputData data))
		{
			data.Direction.Normalize(); // prevents cheating, not that we care
			_cc.Move(5 * data.Direction * Runner.DeltaTime);
		}
	}
}

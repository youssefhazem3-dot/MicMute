namespace MicMute;

public class AudioDevice
{
	public string Id { get; }

	public string Name { get; }

	public AudioDevice(string id, string name)
	{
		Id = id;
		Name = name;
	}

	public override string ToString()
	{
		return Name;
	}
}

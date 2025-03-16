namespace Framework.Interfaces;

/* Interface for defining serialization operations on any game type data */

public interface IGameSerializer
{
    string Serialize(); // convert game state data to any format (string, JSON, etc.)
    void Deserialize(string serializedData); // reconstruct game state from any serialized data format
}

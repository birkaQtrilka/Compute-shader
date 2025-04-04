using UnityEditor;

[CustomEditor(typeof(Isolating), true)]
public class GeneralIsolator : Isolator<Isolating>
{
    protected override bool enabled => true;
}

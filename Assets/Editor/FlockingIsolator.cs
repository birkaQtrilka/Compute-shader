using UnityEditor;

/**
 * This script makes sure only one view is ever active in the inspector.
 * If you don't want that, set enabled to false.
 */
[CustomEditor(typeof(FlockingBase), true)]
public class FlockingIsolator : Isolator<FlockingBase>
{
    protected override bool enabled => true;
}


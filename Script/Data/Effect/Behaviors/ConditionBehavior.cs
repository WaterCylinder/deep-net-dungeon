using Godot;
using System;

[GlobalClass]
public partial class ConditionBehavior : Behavior
{
    
    public bool True()
    {
        return true;
    }
    public bool False()
    {
        return false;
    }
}

using Godot;
using System;

public abstract partial class EntityComponent : Node2D
{
    [Export] public Entity entity;

    public virtual void Init(Entity entity)
    {
        this.entity = entity;
        _Init();
    }

    protected abstract void _Init();
}

using System;

[Serializable]
public class MemoryRecord3D
{
    public MemoryData3D memory;
    public bool restored;
    public MemoryDecision3D decision;

    public MemoryRecord3D(MemoryData3D memory)
    {
        this.memory = memory;
        restored = false;
        decision = MemoryDecision3D.Unchosen;
    }
}

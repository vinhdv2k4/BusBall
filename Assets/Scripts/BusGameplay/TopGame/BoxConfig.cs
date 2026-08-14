using System;
using System.Collections.Generic;

[Serializable]
public class BoxConfig
{
    public ColorType ColorType;
    public List<BoxMechanicConfig> MechanicData = new();
}

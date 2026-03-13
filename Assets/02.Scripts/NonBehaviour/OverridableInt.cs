using System;

[Serializable]
public class OverridableInt
{
    public bool UseOverride = false;
    public int OverrideValue;
    
    public int GetValue(int baseValue) => UseOverride ? OverrideValue : baseValue;
}
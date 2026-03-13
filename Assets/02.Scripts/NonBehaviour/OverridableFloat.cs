using System;

[Serializable]
public class OverridableFloat
{
    public bool UseOverride = false;
    public float OverrideValue;
    
    public float GetValue(float baseValue) => UseOverride ? OverrideValue : baseValue;

}
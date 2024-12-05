using Microsoft.SemanticKernel;
using System.ComponentModel;

public class BrightnessConverterPlugin
{
    [KernelFunction("brightness_percentage_converter")]
    [Description("Converts a percentage to an acceptable light brightness value from 0 to 255")]
    [return: Description("Corrected Brightness value for a light")]
    public  int ConvertBrightnessPercentage(int percentage)
    {
        //change the scale from 0-100 to 0-255
        return (int)(percentage * 2.55);
    }
}

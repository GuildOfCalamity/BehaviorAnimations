using Microsoft.UI.Xaml;
using System.Linq;

namespace BehaviorAnimations;

/// <summary>
/// A way to use converters via function binding.
/// </summary>
/// <remarks>
/// https://github.com/AndrewKeepCoding/FunctionBindingSampleApp/blob/main/FunctionBindingSampleApp/MainPage.xaml.cs
/// </remarks>
public static class Functions
{
    public static Visibility TrueToVisible(bool value)
    {
        return value is true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public static Visibility NotEmptyStringToVisible(string value)
    {
        return string.IsNullOrEmpty(value) is not true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public static Visibility AllTrueToVisible(bool? value1, bool? value2, bool? value3)
    {
        return value1 is true && value2 is true && value3 is true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public static Visibility AnyTrueToVisible(bool? value1, bool? value2, bool? value3)
    {
        return value1 is true || value2 is true || value3 is true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public static Visibility TrueToVisible(bool isAnd, bool? value1, bool? value2, bool? value3)
    {
        return isAnd
            ? AllTrueToVisible(value1, value2, value3)
            : AnyTrueToVisible(value1, value2, value3);
    }

    public static Visibility TrueToVisible(params bool[] values) => TrueToVisible(values.All(value => value));
}

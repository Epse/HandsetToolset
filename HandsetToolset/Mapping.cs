using System;
using System.Reflection;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

namespace HandsetToolset;

public class Mapping(string trigger, InjectedInputKeyboardInfo action)
{
    public string Trigger = trigger;
    public InjectedInputKeyboardInfo Action = action;

    public string ActionText
    {
        get
        {
            var action = (Action.KeyOptions & InjectedInputKeyOptions.KeyUp) != 0 ? "Release" : "Press";
            var keyName =  Enum.GetName(typeof(VirtualKey), Action.VirtualKey) ?? "???";
            return $"{action} {keyName}";
        }
    }
}
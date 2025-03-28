using System;
using System.Reflection;
using System.Text.Json.Serialization;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

namespace HandsetToolset;

public class Mapping(string trigger, InjectedInputKeyboardInfo action)
{
    [JsonInclude, JsonRequired]
    public string Trigger = trigger;
    [JsonInclude, JsonRequired]
    public InjectedInputKeyboardInfo Action = action;

    [JsonIgnore]
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
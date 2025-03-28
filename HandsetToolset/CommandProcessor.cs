using System;
using System.Collections.ObjectModel;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

namespace HandsetToolset
{
    public class CommandProcessor
    {
        private readonly InputInjector _inject = InputInjector.TryCreate();

        public ObservableCollection<Mapping> Mappings { get; private set; }
        private static CommandProcessor? _instance;

        public static CommandProcessor Current
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CommandProcessor();
                }

                return _instance;
            }
        }

        private CommandProcessor()
        {
            Mappings = new ObservableCollection<Mapping>();
            Mappings.Add(new Mapping("+PTT=P", new InjectedInputKeyboardInfo
            {
                    VirtualKey = (ushort)VirtualKey.F13,
                    KeyOptions = InjectedInputKeyOptions.None,
            }));
            Mappings.Add(new Mapping("+PTT=R", new InjectedInputKeyboardInfo
            {
                    VirtualKey = (ushort)VirtualKey.F13,
                    KeyOptions = InjectedInputKeyOptions.KeyUp,
            }));
            Mappings.Add(new Mapping("C:GP*", new InjectedInputKeyboardInfo
            {
                    VirtualKey = (ushort)VirtualKey.F14,
                    KeyOptions = InjectedInputKeyOptions.None,
            }));
            Mappings.Add(new Mapping("C:GR*", new InjectedInputKeyboardInfo
            {
                    VirtualKey = (ushort)VirtualKey.F14,
                    KeyOptions = InjectedInputKeyOptions.KeyUp,
            }));
        }

        public string Process(string input)
        {
            foreach (var mapping in Mappings)
            {
                if (input.Contains(mapping.Trigger))
                {
                    _inject.InjectKeyboardInput(new [] {mapping.Action});
                    return "";
                }
            }

            return input;
        }
    }
}
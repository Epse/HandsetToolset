using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

namespace HandsetToolset
{
    public class CommandProcessor
    {
        private readonly InputInjector _inject = InputInjector.TryCreate();
        private readonly string _storageFile = Path.Combine(App.DataFolder, "commands.json");

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

            if (!LoadFromStorage())
            {
                AddDefaults();
            }
        }

        public void Store()
        {
            Directory.CreateDirectory(App.DataFolder);
            string jsonString = JsonSerializer.Serialize(Mappings.ToList());
            File.WriteAllText(_storageFile, jsonString);
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

        private bool LoadFromStorage()
        {
            if (!File.Exists(_storageFile))
                return false;

            string jsonString = File.ReadAllText(_storageFile);
            try
            {
                List<Mapping>? mappings = JsonSerializer.Deserialize<List<Mapping>>(jsonString);
                if (mappings == null) return false;

                foreach (var mapping in mappings)
                {
                    Mappings.Add(mapping);
                }

                return true;
            }
            catch
            {
                File.Delete(_storageFile);
                return false;
            }
        }

        private void AddDefaults()
        {
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
    }
}
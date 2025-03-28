using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

namespace HandsetToolset
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MappingPage : Page
    {
        private readonly CommandProcessor _processor = CommandProcessor.Current;
        public readonly ObservableCollection<string> Buttons;

        public MappingPage()
        {
            this.InitializeComponent();

            Buttons = new ObservableCollection<string>();
            foreach (var btn in Enum.GetNames(typeof(VirtualKey)))
            {
                Buttons.Add(btn);
            }
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            string trigger = TriggerBox.Text;
            var action = ActionBox.SelectedIndex == 0 ? InjectedInputKeyOptions.None : InjectedInputKeyOptions.KeyUp;
            VirtualKey? button = (VirtualKey?)Enum.GetValues(typeof(VirtualKey)).GetValue(ButtonBox.SelectedIndex);

            if (button == null) return;

            _processor.Mappings.Add(new Mapping(trigger, new InjectedInputKeyboardInfo
            {
                VirtualKey = (ushort)button,
                KeyOptions = action,
            }));

            TriggerBox.Text = "";
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var mapping = (Mapping)((Button)e.OriginalSource).DataContext;
            _processor.Mappings.Remove(mapping);
        }
    }
}
